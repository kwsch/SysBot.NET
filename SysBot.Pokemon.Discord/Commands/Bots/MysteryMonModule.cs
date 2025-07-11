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
    public class SurprisePokemonModule<T> : ModuleBase<SocketCommandContext> where T : PKM, new()
    {
        private static TradeQueueInfo<T> Info => SysCord<T>.Runner.Hub.Queues.Info;
        private static readonly Random rng = new(); // Global RNG for consistent randomness
        private static readonly HashSet<ushort> BannedForms = new() // Excludes specific forms that are not legal
        {
            800, // Necrozma
            888, // Zacian
            889, // Zamazenta
        };

        // Commands for trading random Pokémon with completely random attributes and stats
        [Command("mysterymon")]
        [Alias("mm", "mystery", "surprise")]
        [Summary("Trades a random Pokémon with completely random attributes and stats.")]
        public async Task TradeRandomPokemonAsync()
        {
            var userID = Context.User.Id;
            if (Info.IsUserInQueue(userID))
            {
                await ReplyAsync("You already have an existing trade in the queue. Please wait until it is processed.").ConfigureAwait(false);
                return;
            }
            var code = Info.GetRandomTradeCode(userID, Context.Channel, Context.User);
            await Task.Run(async () =>
            {
                await TradeRandomPokemonAsync(code).ConfigureAwait(false);
            }).ConfigureAwait(false);
        }

        // Commands for trading random Pokémon with completely random attributes and stats (with a specific trade code)
        [Command("mysterymon")]
        [Alias("mm", "mystery", "surprise")]
        [Summary("Trades a random Pokémon with completely random attributes and stats.")]
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

                    ushort speciesId = speciesList[rng.Next(speciesList.Count)];

                    // Prevent illegal form-only species here
                    if (BannedForms.Contains(speciesId))
                        continue;

                    var randomIndex = new Random().Next(speciesList.Count);
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

                            // LOGGING
                            var sName = GameInfo.GetStrings("en").specieslist[pk.Species];
                            var ivs = $"{pk.IV_HP}/{pk.IV_ATK}/{pk.IV_DEF}/{pk.IV_SPA}/{pk.IV_SPD}/{pk.IV_SPE}";
                            var evs = $"{pk.EV_HP}/{pk.EV_ATK}/{pk.EV_DEF}/{pk.EV_SPA}/{pk.EV_SPD}/{pk.EV_SPE}";
                            var moves = string.Join(", ", pk.Moves.Select(m => GameInfo.GetStrings("en").movelist[m]));
                            Console.WriteLine($"[MysteryMon] {sName} | Lv {pk.CurrentLevel} | Shiny: {pk.IsShiny} | IVs: {ivs} | EVs: {evs} | Ability #: {pk.AbilityNumber} | Item: {pk.HeldItem} | Moves: {moves}");

                        }
                    }
                }

                var sig = Context.User.GetFavor();
                await AddTradeToQueueAsync(code, Context.User.Username, pk, sig, Context.User).ConfigureAwait(false);

                if (Context.Message is IUserMessage userMessage)
                {
                    _ = DeleteMessageAfterDelay(userMessage, 2000);
                }
            }
            catch (Exception ex)
            {
                LogUtil.LogSafe(ex, nameof(SurprisePokemonModule<T>));
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
            var heldItems = GetHeldItemPool();
            if (gameVersion != GameVersion.PLA)
                pk.HeldItem = heldItems[rng.Next(heldItems.Count)];

            //--------------- Fateful Encounter --------------//
            pk.FatefulEncounter = random.Next(0, 100) < 12; // 12% chance of being "True" for being an Event/Gift
            if (!pk.FatefulEncounter)
            {
                //--------------- Shiny --------------//
                if (rng.Next(0, 100) < 50) // 50% chance of being shiny
                    pk.SetShiny(); // Make Shiny based on odds
            }

            //--------------- Level --------------//
            bool isLevel100 = random.Next(0, 100) < 10; // 10% chance of being Level 100 (Not really though, more like 20%)
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
                bool isPerfectIVs = random.Next(0, 100) < 30; // 30% chance of having perfect IVs if not Level 100
                if (isPerfectIVs)
                {
                    // Set all IVs to 31
                    pk.IV_HP = 31;  // HP
                    pk.IV_ATK = 31; // Attack
                    pk.IV_DEF = 31; // Defense
                    pk.IV_SPA = 31; // Special Attack
                    pk.IV_SPD = 31; // Special Defense
                    pk.IV_SPE = 31; // Speed
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
                // Do not let EVs exceed 252 for each individual entry
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
                totalEVs += ev; // Update the total EVs into final set
            }

            //--------------- Ability --------------//
            int abilityCount = pk.PersonalInfo.AbilityCount; // Get the number of abilities for the species
            if (abilityCount > 0) // If the species has abilities
            {
                int randomAbilityIndex = rng.Next(abilityCount); // Tries to get a random ability index
                pk.RefreshAbility(randomAbilityIndex); // Set the ability
            }

            //--------------- Tera Type for SV --------------//
            if (pk is PK9 pk9)
            {
                var personal = pk9.PersonalInfo; // Get the info for SV
                int type1 = personal.Type1; // Primary Type
                int type2 = personal.Type2; // Secondary Type

                var typePool = Enumerable.Range(0, 18) // Randomly select a Tera Type from 1-18
                    .Where(t => t != type1 && t != type2) // Exclude the primary and secondary types
                    .ToList();

                int newTeraTypeIndex = typePool[rng.Next(typePool.Count)]; // Randomly select a Tera Type from the pool
                pk9.SetTeraType((MoveType)newTeraTypeIndex); // Set the Tera Type
                string teraTypeName = ((MoveType)newTeraTypeIndex).ToString(); // Get the name of the Tera Type

            }
        }

        //--------------- Held Item Pool --------------//
        private static List<int> GetHeldItemPool() => new()
        {
            0,1,236,244,1120,218,286,217,328,221,248,255,228,229,230,275,233,281,
            541,234,265,269,245,538,645,1606,223,287,297,220,270,290,294,241,268,
            1128,50,55,47,48,49,51,54,158,210,155,157,619,620,82,84,85,109,81,80,
            851,83,107,108,109,110
        };

        //--------------- Supported Game Versions --------------//
        private static GameVersion GetGameVersion()
        {
            return typeof(T) switch
            {
                Type t when t == typeof(PK8) => GameVersion.SWSH,
                Type t when t == typeof(PB8) => GameVersion.BDSP,
                Type t when t == typeof(PA8) => GameVersion.PLA,
                Type t when t == typeof(PK9) => GameVersion.SV,
                _ => throw new ArgumentException("Unsupported game version.")
            };
        }

        //--------------- Get Species List --------------//
        public static List<ushort> GetSpecies(GameVersion gameVersion, string language = "en")
        {
            var gameStrings = GameInfo.GetStrings(language);
            var pt = GetPersonalTable(gameVersion);
            return gameStrings.specieslist
                .Select((name, index) => (name, index))
                .Where(x => !string.IsNullOrWhiteSpace(x.name) && !BannedForms.Contains((ushort)x.index))
                .Select(x => (ushort)x.index)
                .ToList();
        }

        //--------------- Get Personal Table --------------//
        private static object GetPersonalTable(GameVersion gameVersion) => gameVersion switch
        {
            GameVersion.SWSH => PersonalTable.SWSH,
            GameVersion.BDSP => PersonalTable.BDSP,
            GameVersion.PLA => PersonalTable.LA,
            GameVersion.SV => PersonalTable.SV,
            _ => throw new ArgumentException("Unsupported personal table type.")
        };

        /////////////////////////////////////////////////////////////////
        ///// Add this mess of a Pokemon to the queue for processing ////
        /////////////////////////////////////////////////////////////////

        private async Task AddTradeToQueueAsync(int code, string trainerName, T? pk, RequestSignificance sig, SocketUser usr, bool isBatchTrade = false, int batchTradeNumber = 1, int totalBatchTrades = 1, bool isHiddenTrade = false)
        {
            var la = new LegalityAnalysis(pk);
            if (!la.Valid) return; // Should never happen with retry logic

            await QueueHelper<T>.AddToQueueAsync(Context, code, trainerName, sig, pk, PokeRoutineType.LinkTrade, PokeTradeType.Specific, usr, isBatchTrade, batchTradeNumber, totalBatchTrades, isHiddenTrade).ConfigureAwait(false);
        }
    }
}
