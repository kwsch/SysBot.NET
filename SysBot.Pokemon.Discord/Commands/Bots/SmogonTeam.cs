using Discord;
using Discord.Commands;
using PKHeX.Core;
using PKHeX.Core.AutoMod;
using SysBot.Pokemon.Helpers;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using DiscordColor = Discord.Color;

namespace SysBot.Pokemon.Discord
{
    public class SmogonTeam : ModuleBase<SocketCommandContext>
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

        [Command("randomteam")]
        [Alias("rt", "Rt", "RandomTeam", "smogonteam", "st")]
        [Summary("Generates a Smogon team and sends it as files via DM.")]
        public async Task GenerateSmogonTeamAsync(string gameVersion, [Remainder] string? type = null)
        {
            var generatingMessage = await ReplyAsync("Generating and sending your Smogon team. Please wait...");
            try
            {
                var team = GenerateSmogonTeam(gameVersion, type);

                var format = gameVersion.ToLowerInvariant() switch
                {
                    "bdsp" => 8,
                    "swsh" => 8,
                    "sv" => 9,
                    _ => throw new Exception("Invalid game version."),
                };

                var namer = new GengarNamer();
                var pokemonImages = new List<System.Drawing.Image>();

                foreach (var set in team)
                {
                    var speciesName = GameInfo.GetStrings("en").Species[set.Species];
                    var pk = GetPKM(gameVersion, set.Species);

                    if (pk == null)
                    {
                        await ReplyAsync($"Error generating {speciesName}.");
                        continue;
                    }

                    // Get the Pokémon image
                    bool canGmax = pk is PK8 pk8 && pk8.CanGigantamax;
                    string speciesImageUrl = GetNonShinyImageUrl(gameVersion, pk);
                    var speciesImage = System.Drawing.Image.FromStream(await new HttpClient().GetStreamAsync(speciesImageUrl));
                    pokemonImages.Add(speciesImage);
                }

                var combinedImage = CombineImages(pokemonImages);

                using var memoryStream = new MemoryStream();
                using (var archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, true))
                {
                    foreach (var set in team)
                    {
                        var speciesName = GameInfo.GetStrings("en").Species[set.Species];
                        var pk = GetPKM(gameVersion, set.Species);

                        if (pk == null)
                        {
                            await ReplyAsync($"Error generating {speciesName}.");
                            continue;
                        }

                        // Legalize the PKM
                        var sav = AutoLegalityWrapper.GetTrainerInfo(pk.Format);
                        var template = AutoLegalityWrapper.GetTemplate(set);
                        pk = sav.GetLegal(template, out _);

                        // Generate the file name using GengarNamer
                        var fileName = namer.GetName(pk);
                        var entry = archive.CreateEntry($"{fileName}.{pk.Extension}");
                        using var entryStream = entry.Open();
                        await entryStream.WriteAsync(pk.Data.AsMemory(0, pk.Data.Length));
                    }
                }

                memoryStream.Position = 0;

                // Send the ZIP file to the user's DM
                await Context.User.SendFileAsync(memoryStream, $"team.zip");

                // Save the combined image as a file
                combinedImage.Save("team.png");
                using (var imageStream = new MemoryStream())
                {
                    combinedImage.Save(imageStream, System.Drawing.Imaging.ImageFormat.Png);
                    imageStream.Position = 0;

                    // Send the combined image file with an embed to the channel
                    DiscordColor embedColor;
                    string typeName;

                    if (type is null)
                    {
                        embedColor = DiscordColor.Orange;
                        typeName = "Random Team";
                    }
                    else
                    {
                        var typeNames = GameInfo.GetStrings(gameVersion).types;
                        var typeInfo = typeNames.FirstOrDefault(t => string.Equals(t, type, StringComparison.OrdinalIgnoreCase));
                        if (typeInfo is null)
                        {
                            embedColor = DiscordColor.Orange;
                            typeName = "Random Team";
                        }
                        else
                        {
                            embedColor = GetTypeColor(typeInfo);
                            typeName = $"{type.ToUpper()} Team";
                        }
                    }

                    var embedBuilder = new EmbedBuilder()
                        .WithColor(embedColor)
                        .WithAuthor(
                            author =>
                            {
                                author
                                    .WithName($"{Context.User.Username}'s Generated {typeName}")
                                    .WithIconUrl(Context.User.GetAvatarUrl() ?? Context.User.GetDefaultAvatarUrl());
                            })
                        .WithImageUrl($"attachment://team.png")
                        .WithFooter($"Legalized Team Sent to {Context.User.Username}'s Inbox")
                        .WithCurrentTimestamp();
                    var embed = embedBuilder.Build();

                    var embedMessage = await Context.Channel.SendFileAsync(imageStream, "team.png", embed: embed);

                    // Clean up the messages after 10 seconds
                    await Task.Delay(10000);
                    await generatingMessage.DeleteAsync();
                    if (Context.Message is IUserMessage userMessage)
                        await userMessage.DeleteAsync().ConfigureAwait(false);
                }

                // Clean up the temporary image file
                File.Delete("team.png");

            }
            catch (Exception ex)
            {
                await ReplyAsync($"Error generating Smogon team: {ex.Message}");
            }
        }

        private static int GetRandomSpecies(string gameVersion, string type, HashSet<int> addedSpecies)
        {
            var speciesNames = GameInfo.GetStrings(gameVersion).Species;
            var filteredSpecies = string.IsNullOrEmpty(type)
                ? speciesNames.Skip(1).ToArray()
                : speciesNames.Skip(1).Where(s => IsPokemonOfType(gameVersion, Array.IndexOf((Array)speciesNames, s), type)).ToArray();

            var availableSpecies = filteredSpecies
                .Where(s => !addedSpecies.Contains(Array.IndexOf((Array)speciesNames, s)))
                .ToArray();

            if (availableSpecies.Length == 0)
            {
                throw new Exception("No more unique species available.");
            }

            var random = new Random();
            var randomIndex = random.Next(0, availableSpecies.Length);
            var speciesName = availableSpecies[randomIndex];

            var sanitizedSpeciesName = speciesName.Replace(" ", "").Replace("-", "").Replace(".", "");
            var species = Enum.GetValues(typeof(Species))
                .Cast<Species>()
                .FirstOrDefault(s => string.Equals(s.ToString(), sanitizedSpeciesName, StringComparison.OrdinalIgnoreCase));

            if (species != default)
            {
                // Check if the Pokémon requires a Home Tracker
                var pk = GetPKM(gameVersion, (int)species);
                if (pk == null)
                {
                    return -1;
                }

                var current = GetEntityContext(gameVersion);

                if (RequiresHomeTracker(pk, current))
                {
                    // Pokémon requires a Home Tracker
                    return -1;
                }

                return (int)species;
            }

            return -1;
        }

        private static EntityContext GetEntityContext(string gameVersion)
        {
            return gameVersion.ToLowerInvariant() switch
            {
                "bdsp" => EntityContext.Gen8b,
                "swsh" => EntityContext.Gen8,
                "sv" => EntityContext.Gen9,
                _ => EntityContext.None,
            };
        }

        private static bool RequiresHomeTracker(PKM pk, EntityContext current)
        {
            var context = pk.Context;
            return HomeTrackerUtil.IsRequired(context, current);
        }

        private static List<ShowdownSet> GenerateSmogonTeam(string gameVersion, string type)
        {
            var random = new Random();
            var team = new List<ShowdownSet>();
            var addedSpecies = new HashSet<int>();

            while (team.Count < 6)
            {
                var species = GetRandomSpecies(gameVersion, type, addedSpecies);

                if (species == -1)
                {
                    continue;
                }

                var pk = GetPKM(gameVersion, species);

                if (pk == null)
                {
                    continue;
                }

                var generator = new SmogonSetGenerator(pk);

                if (!generator.Valid)
                {
                    continue;
                }

                var sets = generator.Sets;
                if (sets.Count == 0)
                {
                    continue;
                }

                var randomIndex = random.Next(0, sets.Count);
                var randomSet = sets[randomIndex];
                team.Add(randomSet);
                addedSpecies.Add(species);
            }

            return team;
        }

        private static PKM? GetPKM(string gameVersion, int species)
        {
            return gameVersion.ToLowerInvariant() switch
            {
                "bdsp" => new PB8 { Species = (ushort)species },
                "swsh" => new PK8 { Species = (ushort)species },
                "sv" => new PK9 { Species = (ushort)species },
                _ => null,
            };
        }

        private static bool IsPokemonOfType(string gameVersion, int species, string type)
        {
            var types = GameInfo.GetStrings(gameVersion).types;
            var personalInfo = PersonalTable.SV.GetFormEntry((ushort)species, 0);

            return string.Equals(types[personalInfo.Type1], type, StringComparison.OrdinalIgnoreCase)
                || string.Equals(types[personalInfo.Type2], type, StringComparison.OrdinalIgnoreCase);
        }

        private static string GetNonShinyImageUrl(string gameVersion, PKM pk)
        {
            bool canGmax = pk is PK8 pk8 && pk8.CanGigantamax;
            string shinyImageUrl = gameVersion.ToLowerInvariant() switch
            {
                "bdsp" => AbstractTrade<PB8>.PokeImg(pk, false, false),
                "swsh" => AbstractTrade<PK8>.PokeImg(pk, canGmax, false),
                "sv" => AbstractTrade<PK9>.PokeImg(pk, false, false),
                _ => throw new Exception("Invalid game version."),
            };

            // Replace the shiny indicator in the URL with the non-shiny indicator
            string nonShinyImageUrl = shinyImageUrl.Replace("_r.png", "_n.png");

            return nonShinyImageUrl;
        }

        private static DiscordColor GetTypeColor(string typeName)
        {
            return typeName.ToLowerInvariant() switch
            {
                "normal" => new DiscordColor(168, 168, 120), // Beige
                "fighting" => new DiscordColor(192, 48, 40), // Reddish-brown
                "flying" => new DiscordColor(173, 216, 230), // Light blue
                "poison" => new DiscordColor(160, 64, 160), // Purple
                "ground" => new DiscordColor(224, 192, 104), // Tan
                "rock" => new DiscordColor(184, 160, 88), // Brownie
                "bug" => new DiscordColor(168, 184, 32), // Yellowish-green
                "ghost" => new DiscordColor(112, 88, 152), // Dark purple
                "steel" => new DiscordColor(184, 184, 208), // Light gray
                "fire" => new DiscordColor(240, 128, 48), // Orangish-red
                "water" => new DiscordColor(104, 144, 240), // Blue
                "grass" => new DiscordColor(120, 200, 80), // Green
                "electric" => new DiscordColor(248, 208, 48), // Yellow
                "psychic" => new DiscordColor(248, 88, 136), // Hot pink
                "ice" => new DiscordColor(152, 216, 216), // Cyan
                "dragon" => new DiscordColor(112, 56, 248), // Dark red
                "dark" => new DiscordColor(112, 88, 72), // Dark gray
                "fairy" => new DiscordColor(238, 153, 172), // Magic pink
                _ => new DiscordColor(255, 165, 0), // Orange
            };
        }
    }
}
