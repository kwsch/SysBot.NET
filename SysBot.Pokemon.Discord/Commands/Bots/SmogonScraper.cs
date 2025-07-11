using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Discord.Commands;
using HtmlAgilityPack;
using PKHeX.Core;
using System.Collections.Generic;

namespace SysBot.Pokemon.Discord.Commands.Bots
{
    public class SmogonScraper : ModuleBase<SocketCommandContext>
    {
        private static readonly HttpClient client = new HttpClient();
        private static readonly Random random = new Random();

        [Command("smogon")]
        [Summary("Fetches a Smogon set for the specified Pokémon and game.")]
        public async Task ScrapeSmogonSet(string pokemon, string game)
        {
            try
            {
                var set = await GetSmogonSet(pokemon, game);
                if (string.IsNullOrEmpty(set))
                {
                    await ReplyAsync($"No set found for {pokemon} in {game}.").ConfigureAwait(false);
                    return;
                }

                await ReplyAsync($"Smogon set for {pokemon} ({game}):\n```\n{set}\n```").ConfigureAwait(false);

                var pkmn = GeneratePKMFromSmogonSet(set, game);
                if (pkmn != null)
                {
                    await ReplyAsync("Successfully generated a PKM file.").ConfigureAwait(false);
                    // Add your code here to handle the generated PKM object
                }
                else
                {
                    await ReplyAsync("Failed to generate a PKM file.").ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred while scraping the Smogon set: {ex.Message}");
                await ReplyAsync("An error occurred while processing your request. Please try again later.").ConfigureAwait(false);
            }
        }

        private static async Task<string> GetSmogonSet(string pokemon, string game)
        {
            var url = $"https://www.smogon.com/dex/{game}/pokemon/{pokemon}/";
            try
            {
                var response = await client.GetStringAsync(url).ConfigureAwait(false);
                var doc = new HtmlDocument();
                doc.LoadHtml(response);

                var setNodes = doc.DocumentNode.SelectNodes("//pre[contains(@class, 'tooltip-content')]");
                if (setNodes == null || setNodes.Count == 0)
                    return null;

                var sets = setNodes.Select(node => node.InnerText.Trim()).ToList();
                var randomSet = sets[random.Next(sets.Count)];
                return randomSet;
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"HttpRequestException occurred while scraping: {ex.Message}");
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception occurred while scraping: {ex.Message}");
                return null;
            }
        }

        private static PKM GeneratePKMFromSmogonSet(string set, string game)
        {
            var species = ExtractSpeciesFromSet(set);
            if (string.IsNullOrEmpty(species))
                return null;

            PKM pk;
            switch (game.ToLower())
            {
                case "swsh":
                    pk = new PK8();
                    break;
                case "sv":
                    pk = new PK9();
                    break;
                case "bdsp":
                    pk = new PB8();
                    break;
                case "pla":
                    pk = new PA8();
                    break;
                case "lgpe":
                    pk = new PB7();
                    break;
                default:
                    return null;
            }

            var speciesIndex = GameInfo.SpeciesDataSource
                .Select((item, index) => new { item, index })
                .FirstOrDefault(x => x.item.Text.Equals(species, StringComparison.OrdinalIgnoreCase))?.index ?? -1;
            if (speciesIndex < 0)
                return null;

            pk.Species = (ushort)speciesIndex;
            return pk;
        }

        private static string ExtractSpeciesFromSet(string set)
        {
            var lines = set.Split('\n');
            return lines.Length > 0 ? lines[0].Split('@')[0].Trim() : null;
        }
    }
}
