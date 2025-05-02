using Discord;
using Discord.Commands;
using Discord.WebSocket;
using PKHeX.Core;
using SysBot.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SysBot.Pokemon.Discord
{
    public class MysteryMonModule<T> : ModuleBase<SocketCommandContext> where T : PKM, new()
    {
        private static TradeQueueInfo<T> Info => SysCord<T>.Runner.Hub.Queues.Info;

        [Command("mysterymon")]
        [Alias("mm")]
        [Summary("Trades a random Pokémon with perfect stats and shiny appearance.")]
        public async Task TradeRandomPokemonAsync()
        {
            var userID = Context.User.Id;
            if (Info.IsUserInQueue(userID))
            {
                await ReplyAsync("You already have an existing trade in the queue. Please wait until it is processed.").ConfigureAwait(false);
                return;
            }
            var code = Info.GetRandomTradeCode(userID);
            await Task.Run(async () =>
            {
                await TradeRandomPokemonAsync(code).ConfigureAwait(false);
            }).ConfigureAwait(false);
        }

        [Command("mysterymon")]
        [Alias("mm")]
        [Summary("Trades a random Pokémon with perfect stats and shiny appearance.")]
        [RequireQueueRole(nameof(DiscordManager.RolesTrade))]
        public async Task TradeRandomPokemonAsync([Summary("Trade Code")] int code)
        {
            var userID = Context.User.Id;
            if (Info.IsUserInQueue(userID))
            {
                await ReplyAsync("You already have an existing trade in the queue. Please wait until it is processed.").ConfigureAwait(false);
                return;
            }

            try
            {
                T? pk = null;  // Loop logic until legal set found
                bool isValid = false;

                while (!isValid)
                {

                    var gameVersion = GetGameVersion();
                    var speciesList = GetSpecies(gameVersion, "en");

                    var randomIndex = new Random().Next(speciesList.Count);
                    ushort speciesId = speciesList[randomIndex];
                    var speciesName = GameInfo.GetStrings("en").specieslist[speciesId];

                    var showdownSet = new ShowdownSet(speciesName);
                    var template = AutoLegalityWrapper.GetTemplate(showdownSet);

                    var sav = AutoLegalityWrapper.GetTrainerInfo<T>();
                    var pkm = sav.GetLegal(template, out var result);

                    RandomizePokemon(pkm, gameVersion);

                    pkm = EntityConverter.ConvertToType(pkm, typeof(T), out _) ?? pkm;

                    if (pkm is T generatedPk)
                    {
                        var la = new LegalityAnalysis(generatedPk);
                        if (la.Valid)
                        {
                            pk = generatedPk;
                            isValid = true;
                        }
                    }
                }

                var sig = Context.User.GetFavor();
                await AddTradeToQueueAsync(code, Context.User.Username, pk, sig, Context.User, isMysteryMon: true).ConfigureAwait(false);

                if (Context.Message is IUserMessage userMessage)
                {
                    _ = DeleteMessageAfterDelay(userMessage, 2000);
                }
            }
            catch (Exception ex)
            {
                LogUtil.LogSafe(ex, nameof(MysteryMonModule<T>));
                await ReplyAsync("An error occurred while processing the request.").ConfigureAwait(false);
            }
        }

        private static async Task DeleteMessageAfterDelay(IUserMessage message, int delayMilliseconds)
        {
            await Task.Delay(delayMilliseconds).ConfigureAwait(false);
            await message.DeleteAsync().ConfigureAwait(false);
        }



        /////////////////////////////////////////////////////////////////
        //////////////////// RANDOMIZE POKEMON INFO /////////////////////
        /////////////////////////////////////////////////////////////////

        private static void RandomizePokemon(PKM pk, GameVersion gameVersion) // Method to call and set other randomizations below
        {
            var random = new Random();

            //--------------- Held Items --------------//
            List<int> heldItems = new List<int>
            {
                  0, // Nothing
                  1, // Master Ball
                236, // Light Ball
                244, // Sharp Beak
               1120, // Heavy-Duty Boots
                218, // Soothe Bell
                286, // Grip Claw
                217, // Quick Claw
                328, // Razor Claw
                221, // King’s Rock
                248, // Twisted Spoon
                255, // Shell Bell
                228, // Smoke Ball
                229, // Everstone
                230, // Focus Band
                275, // Focus Sash
                233, // Metal Coat
                281, // Black Sludge
                541, // Air Balloon
                234, // Leftovers
                265, // Wide Lens
                269, // Light Clay
                245, // Mystic Water
                538, // Eviolite
                645, // Ability Capsule
               1606, // Ability Patch
                223, // Amulet Coin
                287, // Choice Scarf
                297, // Choice Specs
                220, // Choice Band
                270, // Life Orb
                290, // Sticky Barb
                294, // Power Band
                241, // Black Belt
                268, // Expert Belt
               1128, // Exp. Candy XL
                 50, // Rare Candy
                 55, // PP Max
                 47, // HP Up
                 48, // Protein
                 49, // Iron
                 50, // Carbos
                 51, // Calcium
                 54, // Zinc
                158, // Sitrus Berry
                210, // Custap Berry
                155, // Oran Berry
                157, // Lum Berry
                619, // Dark Stone
                620, // Light Stone
                 82, // Fire Stone
                 84, // Thunder Stone
                 85, // Water Stone
                109, // Dawn Stone
                 81, // Moon Stone
                 80, // Sun Stone
                851, // Ice Stone
                 83, // Thunder Stone
                107, // Shiny Stone
                108, // Dusk Stone
                109, // Dawn Stone
                110  // Oval Stone
            };

            if (gameVersion != GameVersion.PLA)
            {
                pk.HeldItem = heldItems[random.Next(heldItems.Count)];
            }

            //--------------- Fateful Encounter --------------//
            pk.FatefulEncounter = random.Next(0, 100) < 12; // 12% chance of being "True" for being an Event/Gift
            if (!pk.FatefulEncounter)
            {
                //--------------- Shiny --------------//
                bool isShiny = random.Next(0, 100) < 65; // 65% chance of being shiny
                if (isShiny)
                {
                    pk.SetShiny(); // Make shiny
                }

                //--------------- Level --------------//
                bool isLevel100 = random.Next(0, 100) < 5; // 5% chance of being Level 100 (Not really though, more like 20%)
                if (isLevel100)
                {
                    pk.CurrentLevel = 100; // Set to Level 100

                    // Set all IVs to 31 if generated Pokemon is Level 100
                    pk.IV_HP = 31;  // HP
                    pk.IV_ATK = 31; // Attack
                    pk.IV_DEF = 31; // Defense
                    pk.IV_SPA = 31; // Special Attack
                    pk.IV_SPD = 31; // Special Defense
                    pk.IV_SPE = 31; // Speed
                }
                else
                {
                    pk.CurrentLevel = (byte)random.Next(1, 100); // Randomize Levels from 1-99

                    //--------------- IVs --------------//
                    bool isPerfectIVs = random.Next(0, 100) < 15; // 15% chance of having perfect IVs if not Level 100
                    if (isPerfectIVs)
                    {
                        // Set all IVs to 31
                        pk.IV_HP = 31;
                        pk.IV_ATK = 31;
                        pk.IV_DEF = 31;
                        pk.IV_SPA = 31;
                        pk.IV_SPD = 31;
                        pk.IV_SPE = 31;
                    }
                    else
                    {
                        // Otherwise, set IVs randomly
                        pk.IV_HP = (byte)random.Next(0, 32);
                        pk.IV_ATK = (byte)random.Next(0, 32);
                        pk.IV_DEF = (byte)random.Next(0, 32);
                        pk.IV_SPA = (byte)random.Next(0, 32);
                        pk.IV_SPD = (byte)random.Next(0, 32);
                        pk.IV_SPE = (byte)random.Next(0, 32);
                    }
                }

                //--------------- EVs --------------//
                int totalEVs = 0; // Initialize total EV count
                for (int i = 0; i < 6; i++) // There are 6 EVs
                {
                    int ev = random.Next(0, 253); // Random EV values between 0 and 252
                    if (totalEVs + ev > 510) // Total EVs cannot exceed 510
                    {
                        ev = 510 - totalEVs; // Keep EVs within the 510 limit
                    }
                    // Do not let EVs exceed 252
                    if (ev > 252)
                    {
                        ev = 252;
                    }
                    // Register the random EVs to stats
                    switch (i)
                    {
                        case 0: // HP
                            pk.EV_HP = (byte)ev;
                            break;
                        case 1: // Attack
                            pk.EV_ATK = (byte)ev;
                            break;
                        case 2: // Defense
                            pk.EV_DEF = (byte)ev;
                            break;
                        case 3: // Special Attack
                            pk.EV_SPA = (byte)ev;
                            break;
                        case 4: // Special Defense
                            pk.EV_SPD = (byte)ev;
                            break;
                        case 5: // Speed
                            pk.EV_SPE = (byte)ev;
                            break;
                    }
                    totalEVs += ev; // Update the total EVs
                }

                //--------------- Ability --------------//
                var randomAbility = new Random(); // Initialize random number generator
                byte[] validAbilityNumbers = { 1, 2, 4 }; // Valid ability numbers
                byte abilityNumber = validAbilityNumbers[randomAbility.Next(validAbilityNumbers.Length)]; // Select a random valid ability number from above
                pk.AbilityNumber = abilityNumber; // Assign the selected ability number to the Pokemon

            }
        }

        private static GameVersion GetGameVersion()
        {
            if (typeof(T) == typeof(PK8))
                return GameVersion.SWSH;
            else if (typeof(T) == typeof(PB8))
                return GameVersion.BDSP;
            else if (typeof(T) == typeof(PA8))
                return GameVersion.PLA;
            else if (typeof(T) == typeof(PK9))
                return GameVersion.SV;
            else
                throw new ArgumentException("Unsupported game version.");
        }

        public static List<ushort> GetSpecies(GameVersion gameVersion, string language = "en")
        {
            var gameStrings = GameInfo.GetStrings(language);
            var availableSpeciesList = gameStrings.specieslist
                .Select((name, index) => (Name: name, Index: index))
                .Where(item => item.Name != string.Empty)
                .ToList();

            var speciesList = new List<ushort>();
            var pt = GetPersonalTable(gameVersion);
            foreach (var species in availableSpeciesList)
            {
                var speciesId = (ushort)species.Index;
                speciesList.Add(speciesId);
            }

            return speciesList;
        }

        private static PersonalInfo GetFormEntry(object personalTable, ushort species, byte form)
        {
            return personalTable switch
            {
                PersonalTable9SV pt => pt.GetFormEntry(species, form),
                PersonalTable8SWSH pt => pt.GetFormEntry(species, form),
                PersonalTable8LA pt => pt.GetFormEntry(species, form),
                PersonalTable8BDSP pt => pt.GetFormEntry(species, form),
                _ => throw new ArgumentException("Unsupported personal table type."),
            };
        }

        private static object GetPersonalTable(GameVersion gameVersion)
        {
            return gameVersion switch
            {
                GameVersion.SWSH => PersonalTable.SWSH,
                GameVersion.BDSP => PersonalTable.BDSP,
                GameVersion.PLA => PersonalTable.LA,
                GameVersion.SV => PersonalTable.SV,
                _ => throw new ArgumentException("Unsupported game version."),
            };
        }

        private async Task AddTradeToQueueAsync(int code, string trainerName, T? pk, RequestSignificance sig, SocketUser usr, bool isBatchTrade = false, int batchTradeNumber = 1, int totalBatchTrades = 1, bool isHiddenTrade = false, bool isMysteryMon = false)
        {
            var la = new LegalityAnalysis(pk);
            if (!la.Valid)
            {
                string responseMessage;
                string speciesName = GameInfo.GetStrings("en").specieslist[pk.Species];
                responseMessage = $"Use the command again!";

                var reply = await ReplyAsync(responseMessage).ConfigureAwait(false);
                await Task.Delay(6000);
                await reply.DeleteAsync().ConfigureAwait(false);
                return;
            }

            await QueueHelper<T>.AddToQueueAsync(Context, code, trainerName, sig, pk, PokeRoutineType.LinkTrade, PokeTradeType.Specific, usr, isBatchTrade, batchTradeNumber, totalBatchTrades, isHiddenTrade, isMysteryMon).ConfigureAwait(false);
        }
    }
}
