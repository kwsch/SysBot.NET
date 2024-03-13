using Discord;
using Discord.Commands;
using PKHeX.Core;
using System.Web;
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
    public class VGCTeam : ModuleBase<SocketCommandContext>
    {
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

        [Command("vgcteam")]
        [Alias("vt", "Vt", "VGCTeam")]
        [Summary("Generates a random VGC team from VictoryRoad and sends it as files via DM.")]
        public async Task GenerateVGCTeamAsync([Remainder] string trainerName = "")
        {
            var generatingMessage = await ReplyAsync("Generating and sending your VGC team. Please wait...");
            try
            {
                var (showdownSets, selectedPokePasteUrl) = await GenerateRandomVGCTeam(trainerName);

                if (showdownSets.Count == 0)
                {
                    await ReplyAsync($"No pokepaste found for trainer: {trainerName}");
                    return;
                }

                var namer = new GengarNamer();
                var pokemonImages = new List<System.Drawing.Image>();

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
                            var fileName = namer.GetName(pk); // Use GengarNamer to generate the file name
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
                await Context.User.SendFileAsync(memoryStream, $"vgcteam.zip");

                // Save the combined image as a file
                combinedImage.Save("vgcteam.png");
                using (var imageStream = new MemoryStream())
                {
                    combinedImage.Save(imageStream, System.Drawing.Imaging.ImageFormat.Png);
                    imageStream.Position = 0;

                    // Get the Regulation Set from the main page
                    var mainPageUrl = "https://victoryroadvgc.com/sv-rental-teams/"; // Replace with the actual main page URL
                    var httpClient = new HttpClient();
                    var html = await httpClient.GetStringAsync(mainPageUrl);
                    var regex = new Regex(@"<h2 class=""elementor-heading-title elementor-size-default""><div class=""title"" style=""font-weight:500"">(.*?)</div></h2>");
                    var match = regex.Match(html);
                    var regulationSet = match.Success ? match.Groups[1].Value : "Unknown";

                    // Get the Player name, Championship title, and date from the pokepaste
                    var pokePasteUrl = selectedPokePasteUrl;
                    var pokePasteHtml = await httpClient.GetStringAsync(pokePasteUrl);
                    var playerRegex = new Regex(@"<h2>&nbsp;by (.*?)</h2>");
                    var playerMatch = playerRegex.Match(pokePasteHtml);
                    var player = playerMatch.Success ? HttpUtility.HtmlDecode(playerMatch.Groups[1].Value) : "Unknown";

                    var titleRegex = new Regex(@"<h1>(.*?)</h1>");
                    var titleMatch = titleRegex.Match(pokePasteHtml);
                    var title = titleMatch.Success ? HttpUtility.HtmlDecode(titleMatch.Groups[1].Value) : "Unknown";

                    // Send the combined image file with an embed to the channel
                    var embedBuilder = new EmbedBuilder()
                        .WithColor(GetTypeColor())
                        .WithAuthor(
                            author =>
                            {
                                author
                                    .WithName($"{Context.User.Username}'s Generated VGC Team")
                                    .WithIconUrl(Context.User.GetAvatarUrl() ?? Context.User.GetDefaultAvatarUrl());
                            })
                        .WithImageUrl($"attachment://vgcteam.png")
                        .WithFooter($"Legalized Team Sent to {Context.User.Username}'s Inbox")
                        .WithCurrentTimestamp()
                        .AddField("Regulation Set", regulationSet)
                        .AddField("Player", player)
                        .AddField("Title", title);

                    var embed = embedBuilder.Build();

                    var embedMessage = await Context.Channel.SendFileAsync(imageStream, "vgcteam.png", embed: embed);

                    // Clean up the messages after 10 seconds
                    await Task.Delay(10000);
                    await generatingMessage.DeleteAsync();
                    if (Context.Message is IUserMessage userMessage)
                        await userMessage.DeleteAsync().ConfigureAwait(false);
                }

                // Clean up the temporary image file
                File.Delete("vgcteam.png");
            }
            catch (Exception ex)
            {
                await ReplyAsync($"Error generating VGC team: {ex.Message}");
            }
        }

        private static async Task<(List<ShowdownSet> ShowdownSets, string SelectedPokePasteUrl)> GenerateRandomVGCTeam(string trainerName = "")
        {
            var showdownSets = new List<ShowdownSet>();

            // Fetch the HTML content from the VictoryRoad website
            var url = "https://victoryroadvgc.com/sv-rental-teams/";
            var httpClient = new HttpClient();
            var html = await httpClient.GetStringAsync(url);

            // Parse the HTML to extract the pokepaste URLs and corresponding trainer names
            var pokePasteData = ExtractPokePasteData(html);

            string selectedPokePasteUrl;

            if (!string.IsNullOrEmpty(trainerName))
            {
                // Search for the pokepaste URL that matches the specified trainer name
                var matchingPokePasteData = pokePasteData.FirstOrDefault(data => data.TrainerName.Equals(trainerName, StringComparison.OrdinalIgnoreCase));

                if (matchingPokePasteData == default)
                {
                    // No matching pokepaste found for the specified trainer name
                    return (new List<ShowdownSet>(), "");
                }

                selectedPokePasteUrl = matchingPokePasteData.PokePasteUrl;
            }
            else
            {
                // Randomly select a pokepaste URL
                var random = new Random();
                var randomIndex = random.Next(0, pokePasteData.Count);
                selectedPokePasteUrl = pokePasteData[randomIndex].PokePasteUrl;
            }

            // Fetch and parse the showdown sets from the selected pokepaste URL
            var pokePasteHtml = await httpClient.GetStringAsync(selectedPokePasteUrl);
            var sets = ParseShowdownSets(pokePasteHtml);
            showdownSets.AddRange(sets);

            return (showdownSets, selectedPokePasteUrl);
        }

        private static List<(string TrainerName, string PokePasteUrl)> ExtractPokePasteData(string html)
        {
            var pokePasteData = new List<(string TrainerName, string PokePasteUrl)>();

            // Use regex to extract the pokepaste URLs and corresponding trainer names from the HTML
            var regex = new Regex(@"<td style=""border: none;""><b>(.*?)</b>.*?<a href=""(https://pokepast\.es/\w+)""");
            var matches = regex.Matches(html);
            foreach (Match match in matches)
            {
                var trainerName = match.Groups[1].Value;
                var pokePasteUrl = match.Groups[2].Value;
                pokePasteData.Add((trainerName, pokePasteUrl));
            }

            return pokePasteData;
        }

        [Command("pokepaste")]
        [Alias("pp", "Pokepaste", "PP")]
        [Summary("Generates a team from a specified pokepaste URL and sends it as files via DM.")]
        public async Task GenerateVGCTeamFromUrlAsync(string pokePasteUrl)
        {
            var generatingMessage = await ReplyAsync("Generating and sending your Pokepaste team. Please wait...");
            try
            {
                var showdownSets = await GetShowdownSetsFromPokePasteUrl(pokePasteUrl);

                if (showdownSets.Count == 0)
                {
                    await ReplyAsync($"No valid showdown sets found in the pokepaste URL: {pokePasteUrl}");
                    return;
                }

                var namer = new GengarNamer();
                var pokemonImages = new List<System.Drawing.Image>();

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
                            var fileName = namer.GetName(pk); // Use GengarNamer to generate the file name
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
                await Context.User.SendFileAsync(memoryStream, $"pokepasteteam.zip");

                // Save the combined image as a file
                combinedImage.Save("pokepasteteam.png");
                using (var imageStream = new MemoryStream())
                {
                    combinedImage.Save(imageStream, System.Drawing.Imaging.ImageFormat.Png);
                    imageStream.Position = 0;

                    // Send the combined image file with an embed to the channel
                    var embedBuilder = new EmbedBuilder()
                        .WithColor(GetTypeColor())
                        .WithAuthor(
                            author =>
                            {
                                author
                                    .WithName($"{Context.User.Username}'s Generated Team")
                                    .WithIconUrl(Context.User.GetAvatarUrl() ?? Context.User.GetDefaultAvatarUrl());
                            })
                        .WithImageUrl($"attachment://pokepasteteam.png")
                        .WithFooter($"Legalized Team Sent to {Context.User.Username}'s Inbox")
                        .WithCurrentTimestamp();

                    var embed = embedBuilder.Build();

                    var embedMessage = await Context.Channel.SendFileAsync(imageStream, "pokepasteteam.png", embed: embed);

                    // Clean up the messages after 10 seconds
                    await Task.Delay(10000);
                    await generatingMessage.DeleteAsync();
                    if (Context.Message is IUserMessage userMessage)
                        await userMessage.DeleteAsync().ConfigureAwait(false);
                }

                // Clean up the temporary image file
                File.Delete("vgcteam.png");
            }
            catch (Exception ex)
            {
                await ReplyAsync($"Error generating VGC team: {ex.Message}");
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
                // Update the level to 100 in the showdown set text
                showdownText = Regex.Replace(showdownText, @"(?i)(?<=\bLevel: )\d+", "100");
                var set = new ShowdownSet(showdownText);
                showdownSets.Add(set);
            }

            return showdownSets;
        }

        private static DiscordColor GetTypeColor()
        {
            return new DiscordColor(255, 165, 0); // Orange
        }
    }
}
