using Discord;
using Discord.Commands;
using PKHeX.Core;
using SysBot.Pokemon.Helpers;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using DiscordColor = Discord.Color;

namespace SysBot.Pokemon.Discord
{
    public class VGCPastes<T> : ModuleBase<SocketCommandContext> where T : PKM, new()
    {
        // Uses VGCPastes Repository Spreadsheet in which they keep track of all current teams
        // https://twitter.com/VGCPastes
        private async Task<string> DownloadSpreadsheetAsCsv()
        {
            var GID = SysCord<T>.Runner.Config.Trade.VGCPastesConfiguration.GID;
            var csvUrl = $"https://docs.google.com/spreadsheets/d/1axlwmzPA49rYkqXh7zHvAtSP-TKbM0ijGYBPRflLSWw/export?format=csv&gid={GID}";
            using var httpClient = new HttpClient();
            var response = await httpClient.GetAsync(csvUrl);
            response.EnsureSuccessStatusCode();
            var csvData = await response.Content.ReadAsStringAsync();
            return csvData;
        }

        private async Task<List<List<string>>> FetchSpreadsheetData()
        {
            var csvData = await DownloadSpreadsheetAsCsv();
            var rows = csvData.Split('\n');
            var data = rows.Select(row => row.Split(',').Select(cell => cell.Trim('"')).ToList()).ToList();
            return data;
        }

        private static List<(string TrainerName, string PokePasteUrl, string TeamDescription, string DateShared, string RentalCode)> ParsePokePasteData(List<List<string>> data, string pokemonName = null)
        {
            var pokePasteData = new List<(string TrainerName, string PokePasteUrl, string TeamDescription, string DateShared, string RentalCode)>();
            for (int i = 3; i < data.Count; i++)
            {
                var row = data[i];
                if (row.Count > 40) 
                {
                    // Check if a specific Pokémon is requested and if it is present in columns AL-AQ
                    if (pokemonName != null)
                    {
                        var pokemonColumns = row.GetRange(37, 5); // Columns AL-AQ are at indexes 37-41
                        if (!pokemonColumns.Any(cell => cell.Equals(pokemonName, StringComparison.OrdinalIgnoreCase)))
                        {
                            continue; // Skip this row if the specified Pokémon is not found
                        }
                    }
                    var trainerName = row[3].Trim('"');
                    var pokePasteUrl = row[24].Trim('"');
                    var teamDescription = row[1].Trim('"');
                    var dateShared = row[29].Trim('"');
                    var rentalCode = row[28].Trim('"');
                    pokePasteData.Add((trainerName, pokePasteUrl, teamDescription, dateShared, rentalCode));
                }
            }
            return pokePasteData;
        }

        private static (string TrainerName, string PokePasteUrl, string TeamDescription, string DateShared, string RentalCode) SelectRandomTeam(List<(string TrainerName, string PokePasteUrl, string TeamDescription, string DateShared, string RentalCode)> pokePasteData)
        {
            var random = new Random();
            var randomIndex = random.Next(0, pokePasteData.Count);
            return pokePasteData[randomIndex];
        }

        [Command("randomteam")]
        [Alias("rt", "RandomTeam", "Rt")]
        [Summary("Generates a random VGC team from the specified Google Spreadsheet and sends it as files via DM.")]
        public async Task GenerateSpreadsheetTeamAsync(string pokemonName = null)
        {
            // First, check if AllowRequests is true
            if (!SysCord<T>.Runner.Config.Trade.VGCPastesConfiguration.AllowRequests)
            {
                await ReplyAsync("This module is currently disabled.").ConfigureAwait(false);
                return;
            }
            var generatingMessage = await ReplyAsync("Generating and sending your VGC team from VGCPastes. Please wait...");
            try
            {
                // Fetch data from the local CSV file or download it if not available
                var spreadsheetData = await FetchSpreadsheetData();

                // Parse the fetched data
                var pokePasteData = ParsePokePasteData(spreadsheetData, pokemonName);

                // Randomly select a team from the parsed data
                var (TrainerName, PokePasteUrl, TeamDescription, DateShared, RentalCode) = SelectRandomTeam(pokePasteData);

                // Generate and send the team using the existing code from the VGCTeam command
                var showdownSets = await GetShowdownSetsFromPokePasteUrl(PokePasteUrl);

                if (showdownSets.Count == 0)
                {
                    await ReplyAsync($"No valid showdown sets found in the pokepaste URL: {PokePasteUrl}");
                    return;
                }

                var namer = new GengarNamer();
                var pokemonImages = new List<System.Drawing.Image>();
                var (trainerName, pokePasteUrl, teamDescription, dateShared, rentalCode) = SelectRandomTeam(pokePasteData);

                var sanitizedTeamDescription = SanitizeFileName(teamDescription);
                using var memoryStream = new MemoryStream();
                using (var archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, true))
                {
                    foreach (var set in showdownSets)
                    {
                        try
                        {
                            var template = AutoLegalityWrapper.GetTemplate(set);
                            var sav = AutoLegalityWrapper.GetTrainerInfo<PK9>();
                            var pkm = sav.GetLegal(template, out var result);

                            if (pkm is not PK9 pk || !new LegalityAnalysis(pkm).Valid)
                            {
                                var reason = result == "Timeout" ? $"That {GameInfo.Strings.Species[template.Species]} set took too long to generate." :
                                             result == "Failed" ? $"I wasn't able to create a {GameInfo.Strings.Species[template.Species]} from that set." :
                                             "An unknown error occurred.";

                                await ReplyAsync($"Failed to create {GameInfo.Strings.Species[template.Species]}: {reason}");
                                continue;
                            }

                            var speciesName = GameInfo.GetStrings("en").Species[set.Species];
                            var fileName = namer.GetName(pk);
                            var entry = archive.CreateEntry($"{fileName}.{pk.Extension}");
                            using var entryStream = entry.Open();
                            await entryStream.WriteAsync(pk.Data.AsMemory(0, pk.Data.Length));

                            string speciesImageUrl = AbstractTrade<PK9>.PokeImg(pk, false, false);
                            var speciesImage = System.Drawing.Image.FromStream(await new HttpClient().GetStreamAsync(speciesImageUrl));
                            pokemonImages.Add(speciesImage);
                        }
                        catch (Exception ex)
                        {
                            var speciesName = GameInfo.GetStrings("en").Species[set.Species];
                            await ReplyAsync($"An error occurred while processing {speciesName}: {ex.Message}");
                        }
                    }
                }

                var combinedImage = CombineImages(pokemonImages);

                memoryStream.Position = 0;

                // Send the ZIP file to the user's DM
                var zipFileName = $"{sanitizedTeamDescription}.zip";
                await Context.User.SendFileAsync(memoryStream, zipFileName);

                // Save the combined image as a file
                combinedImage.Save("spreadsheetteam.png");
                using (var imageStream = new MemoryStream())
                {
                    combinedImage.Save(imageStream, System.Drawing.Imaging.ImageFormat.Png);
                    imageStream.Position = 0;

                    var embedBuilder = new EmbedBuilder()
                        .WithColor(GetTypeColor())
                        .WithAuthor(
                            author =>
                            {
                                author
                                    .WithName($"{Context.User.Username}'s Generated Team")
                                    .WithIconUrl(Context.User.GetAvatarUrl() ?? Context.User.GetDefaultAvatarUrl());
                            })
                        .WithTitle($"Team: {teamDescription}") 
                        .WithDescription(
                                $"**Trainer Name:** {trainerName}\n" +
                                $"**Date Shared:** {dateShared}\n" +
                                $"{(rentalCode != "None" ? $"**Rental Code:** `{rentalCode}`" : "")}" 
                            )
                        .WithImageUrl($"attachment://spreadsheetteam.png")
                        .WithFooter($"Legalized Team Sent to {Context.User.Username}'s Inbox")
                        .WithCurrentTimestamp();

                    var embed = embedBuilder.Build();

                    var embedMessage = await Context.Channel.SendFileAsync(imageStream, "spreadsheetteam.png", embed: embed);

                    // Clean up the messages after 10 seconds
                    await Task.Delay(10000);
                    await generatingMessage.DeleteAsync();
                    if (Context.Message is IUserMessage userMessage)
                        await userMessage.DeleteAsync().ConfigureAwait(false);
                }

                // Clean up the temporary image file
                File.Delete("spreadsheetteam.png");
            }
            catch (Exception ex)
            {
                await ReplyAsync($"Error generating VGC team from spreadsheet: {ex.Message}");
            }
        }

        private static async Task<List<ShowdownSet>> GetShowdownSetsFromPokePasteUrl(string pokePasteUrl)
        {
            var httpClient = new HttpClient();
            var pokePasteHtml = await httpClient.GetStringAsync(pokePasteUrl);
            var showdownSets = ParseShowdownSets(pokePasteHtml);
            return showdownSets;
        }

        private static List<ShowdownSet> ParseShowdownSets(string pokePasteHtml)
        {
            var showdownSets = new List<ShowdownSet>();
            var regex = new Regex(@"<pre>(.*?)</pre>", RegexOptions.Singleline);
            var matches = regex.Matches(pokePasteHtml);
            foreach (Match match in matches.Cast<Match>())
            {
                var showdownText = match.Groups[1].Value;
                showdownText = System.Net.WebUtility.HtmlDecode(Regex.Replace(showdownText, "<.*?>", string.Empty));
                // Update the level to 100 in the showdown set since some level's don't meet minimum requirements
                showdownText = Regex.Replace(showdownText, @"(?i)(?<=\bLevel: )\d+", "100");
                var set = new ShowdownSet(showdownText);
                showdownSets.Add(set);
            }

            return showdownSets;
        }

        private static System.Drawing.Image CombineImages(List<System.Drawing.Image> images)
        {
            int width = images.Sum(img => img.Width);
            int height = images.Max(img => img.Height);

            Bitmap combinedImage = new Bitmap(width, height);
            using (Graphics g = Graphics.FromImage(combinedImage))
            {
                int offset = 0;
                foreach (System.Drawing.Image img in images)
                {
                    g.DrawImage(img, offset, 0);
                    offset += img.Width;
                }
            }

            return combinedImage;
        }

        private static string SanitizeFileName(string fileName)
        {
            var invalidChars = Path.GetInvalidFileNameChars();
            var validName = string.Join("_", fileName.Split(invalidChars, StringSplitOptions.RemoveEmptyEntries)).TrimEnd('.');
            return validName;
        }

        private static DiscordColor GetTypeColor()
        {
            return new DiscordColor(139, 0, 0); // Dark Red
        }
    }
}
