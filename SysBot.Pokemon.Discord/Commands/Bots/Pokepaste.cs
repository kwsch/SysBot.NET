using Discord;
using Discord.Commands;
using PKHeX.Core;
using SysBot.Base;
using SysBot.Pokemon.Helpers;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using DiscordColor = Discord.Color;

namespace SysBot.Pokemon.Discord
{
    public class Pokepaste : ModuleBase<SocketCommandContext>
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

        [Command("pokepaste")]
        [Alias("pp", "Pokepaste", "PP")]
        [Summary("Generates a team from a specified pokepaste URL and sends it as files via DM.")]
        public async Task GenerateTeamFromUrlAsync(string pokePasteUrl)
        {
            var generatingMessage = await ReplyAsync("Generating and sending your Pokepaste team. Please wait...");
            try
            {
                await Task.Run(async () =>
                {
                    var pokePasteHtml = await Task.Run(() => GetPokePasteHtml(pokePasteUrl)).ConfigureAwait(false);

                    // Extract title from the Pokepaste HTML
                    var titleMatch = Regex.Match(pokePasteHtml, @"<h1>(.*?)</h1>");
                    var title = titleMatch.Success ? titleMatch.Groups[1].Value : "pokepasteteam";
                    // Sanitize the title to make it a valid filename
                    title = Regex.Replace(title, "[^a-zA-Z0-9_.-]", "").Trim();
                    if (title.Length > 30) title = title[..30]; // Truncate if too long

                    var showdownSets = ParseShowdownSets(pokePasteHtml);

                    if (showdownSets.Count == 0)
                    {
                        await ReplyAndDeleteAsync($"No valid showdown sets found in the pokepaste URL: {pokePasteUrl}", 10, generatingMessage).ConfigureAwait(false);
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

                                PK9? pk = null;
                                var timeout = TimeSpan.FromSeconds(10); // Adjust the timeout as needed
                                using (var cts = new CancellationTokenSource(timeout))
                                {
                                    try
                                    {
                                        pk = (PK9?)await Task.Run(() => sav.GetLegal(template, out _), cts.Token).ConfigureAwait(false);
                                    }
                                    catch (OperationCanceledException)
                                    {
                                        await ReplyAndDeleteAsync($"Timeout occurred while generating {GameInfo.Strings.Species[template.Species]}. Skipping...", 10, generatingMessage).ConfigureAwait(false);
                                        continue;
                                    }
                                }

                                if (pk == null || !new LegalityAnalysis(pk).Valid)
                                {
                                    await ReplyAndDeleteAsync($"Failed to create {GameInfo.Strings.Species[template.Species]}. Skipping...", 10, generatingMessage).ConfigureAwait(false);
                                    continue;
                                }

                                var speciesName = GameInfo.GetStrings("en").Species[set.Species];
                                var fileName = namer.GetName(pk); // Use GengarNamer to generate the file name
                                var entry = archive.CreateEntry($"{fileName}.{pk.Extension}");
                                using var entryStream = entry.Open();
                                await entryStream.WriteAsync(pk.Data.AsMemory(0, pk.Data.Length)).ConfigureAwait(false);

                                string speciesImageUrl = AbstractTrade<PK9>.PokeImg(pk, false, false);
                                var speciesImage = await Task.Run(() => System.Drawing.Image.FromStream(new HttpClient().GetStreamAsync(speciesImageUrl).Result)).ConfigureAwait(false);
                                pokemonImages.Add(speciesImage);
                            }
                            catch (Exception ex)
                            {
                                var speciesName = GameInfo.GetStrings("en").Species[set.Species];
                                await ReplyAndDeleteAsync($"An error occurred while processing {speciesName}: {ex.Message}", 10, generatingMessage).ConfigureAwait(false);
                            }
                        }
                    }

                    var combinedImage = CombineImages(pokemonImages);

                    memoryStream.Position = 0;

                    // Send the ZIP file to the user's DM
                    await Context.User.SendFileAsync(memoryStream, $"{title}.zip", text: "Here's your team!").ConfigureAwait(false);

                    // Save the combined image as a file
                    combinedImage.Save($"{title}.png");
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
                            .WithImageUrl($"attachment://{title}.png")
                            .WithFooter($"Legalized Team Sent to {Context.User.Username}'s Inbox")
                            .WithCurrentTimestamp();

                        var embed = embedBuilder.Build();

                        await Context.Channel.SendFileAsync(imageStream, $"{title}.png", embed: embed).ConfigureAwait(false);

                        // Clean up the messages after 10 seconds
                        await DeleteMessagesAfterDelayAsync(generatingMessage, Context.Message, 10).ConfigureAwait(false);
                    }

                    // Clean up the temporary image file
                    File.Delete($"{title}.png");
                }).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                await ReplyAndDeleteAsync($"Error generating Pokepaste team: {ex.Message}", 10, generatingMessage).ConfigureAwait(false);
            }
        }

        private static async Task<string> GetPokePasteHtml(string pokePasteUrl)
        {
            var httpClient = new HttpClient();
            return await httpClient.GetStringAsync(pokePasteUrl);
        }

        private static List<ShowdownSet> ParseShowdownSets(string pokePasteHtml)
        {
            var showdownSets = new List<ShowdownSet>();
            var regex = new Regex(@"<pre>(.*?)</pre>", RegexOptions.Singleline);
            var matches = regex.Matches(pokePasteHtml);
            foreach (Match match in matches)
            {
                var showdownText = match.Groups[1].Value;
                showdownText = System.Net.WebUtility.HtmlDecode(Regex.Replace(showdownText, "<.*?>", string.Empty));
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

        private async Task ReplyAndDeleteAsync(string message, int delaySeconds, IMessage? messageToDelete = null)
        {
            try
            {
                var sentMessage = await ReplyAsync(message).ConfigureAwait(false);
                _ = DeleteMessagesAfterDelayAsync(sentMessage, messageToDelete, delaySeconds);
            }
            catch (Exception ex)
            {
                LogUtil.LogSafe(ex, nameof(Pokepaste));
            }
        }

        private async Task DeleteMessagesAfterDelayAsync(IMessage sentMessage, IMessage? messageToDelete, int delaySeconds)
        {
            try
            {
                await Task.Delay(delaySeconds * 1000).ConfigureAwait(false);
                await sentMessage.DeleteAsync().ConfigureAwait(false);
                if (messageToDelete != null)
                    await messageToDelete.DeleteAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                LogUtil.LogSafe(ex, nameof(Pokepaste));
            }
        }
    }
}
