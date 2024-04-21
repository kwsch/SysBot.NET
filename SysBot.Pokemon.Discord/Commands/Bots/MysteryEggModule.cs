using Discord;
using Discord.Commands;
using Discord.WebSocket;
using PKHeX.Core;
using SysBot.Base;
using SysBot.Pokemon.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SysBot.Pokemon.Discord
{
    public class MysteryEggModule<T> : ModuleBase<SocketCommandContext> where T : PKM, new()
    {
        private static TradeQueueInfo<T> Info => SysCord<T>.Runner.Hub.Queues.Info;

        [Command("mysteryegg")]
        [Alias("me")]
        [Summary("Trades an egg generated from the provided Pokémon name.")]
        public async Task TradeMysteryEggAsync()
        {
            // Check if the user is already in the queue
            var userID = Context.User.Id;
            if (Info.IsUserInQueue(userID))
            {
                await ReplyAsync("You already have an existing trade in the queue. Please wait until it is processed.").ConfigureAwait(false);
                return;
            }
            var code = Info.GetRandomTradeCode(userID);
            await TradeMysteryEggAsync(code).ConfigureAwait(false);
        }

        [Command("mysteryegg")]
        [Alias("me")]
        [Summary("Trades a random mystery egg with perfect stats and shiny appearance.")]
        [RequireQueueRole(nameof(DiscordManager.RolesTrade))]
        public async Task TradeMysteryEggAsync([Summary("Trade Code")] int code)
        {
            // Check if the user is already in the queue
            var userID = Context.User.Id;
            if (Info.IsUserInQueue(userID))
            {
                await ReplyAsync("You already have an existing trade in the queue. Please wait until it is processed.").ConfigureAwait(false);
                return;
            }

            try
            {
                bool validPokemon = false;
                int attempts = 0;
                const int maxAttempts = 15;

                while (!validPokemon && attempts < maxAttempts)
                {
                    attempts++;

                    var sav = AutoLegalityWrapper.GetTrainerInfo<T>();
                    var gameVersion = MysteryEggModule<T>.GetGameVersion();
                    var speciesList = GetBreedableSpecies(gameVersion, "en");

                    var randomIndex = new Random().Next(speciesList.Count);
                    ushort speciesId = speciesList[randomIndex];

                    var context = new EntityContext();
                    var IsEgg = new EncounterEgg(speciesId, 0, 1, 9, gameVersion, context);
                    var pk = IsEgg.ConvertToPKM(sav);

                    SetPerfectIVsAndShiny(pk);

                    pk = EntityConverter.ConvertToType(pk, typeof(T), out _) ?? pk;

                    if (pk is not T pkT)
                    {
                        await ReplyAsync("Oops! I wasn't able to create a mystery egg.  Try again soon.").ConfigureAwait(false);
                        return;
                    }

                    AbstractTrade<T>.EggTrade(pkT, null);

                    var sig = Context.User.GetFavor();
                    validPokemon = await AddTradeToQueueAsync(code, Context.User.Username, pkT, sig, Context.User, isMysteryEgg: true).ConfigureAwait(false);
                }

                if (!validPokemon)
                {
                    await ReplyAsync("Our basket is out of Mystery Eggs at this time, please try again.").ConfigureAwait(false);
                    return;
                }

                if (Context.Message is IUserMessage userMessage)
                {
                    await Task.Delay(2000);
                    await userMessage.DeleteAsync().ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                LogUtil.LogSafe(ex, nameof(MysteryEggModule<T>));
                await ReplyAsync("An error occurred while processing the request.").ConfigureAwait(false);
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

        private static void SetPerfectIVsAndShiny(PKM pk)
        {
            // Set IVs to perfect
            pk.IVs = new[] { 31, 31, 31, 31, 31, 31 };
            // Set as shiny
            pk.SetShiny();
            // Set hidden ability
            pk.RefreshAbility(2);
        }

        public static List<ushort> GetBreedableSpecies(GameVersion gameVersion, string language = "en")
        {
            var gameStrings = GameInfo.GetStrings(language);
            var availableSpeciesList = gameStrings.specieslist
                .Select((name, index) => (Name: name, Index: index))
                .Where(item => item.Name != string.Empty)
                .ToList();

            var breedableSpecies = new List<ushort>();
            var pt = GetPersonalTable(gameVersion);
            foreach (var species in availableSpeciesList)
            {
                var speciesId = (ushort)species.Index;
                var pi = GetFormEntry(pt, speciesId, 0);
                if (IsBreedable(pi) && pi.EvoStage == 1)
                {
                    breedableSpecies.Add(speciesId);
                }
            }

            return breedableSpecies;
        }

        private static bool IsBreedable(PersonalInfo pi)
        {
            return pi.EggGroup1 != 0 || pi.EggGroup2 != 0;
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

        private async Task<bool> AddTradeToQueueAsync(int code, string trainerName, T? pk, RequestSignificance sig, SocketUser usr, bool isBatchTrade = false, int batchTradeNumber = 1, int totalBatchTrades = 1, bool isMysteryEgg = false, List<Pictocodes> lgcode = null, PokeTradeType tradeType = PokeTradeType.Specific, bool ignoreAutoOT = false, bool isHiddenTrade = false)
        {
            lgcode ??= GenerateRandomPictocodes(3);
            if (!pk.CanBeTraded())
            {
                var reply = await ReplyAsync("Provided Pokémon content is blocked from trading!").ConfigureAwait(false);
                await Task.Delay(6000); // Delay for 6 seconds
                await reply.DeleteAsync().ConfigureAwait(false);
                return false;
            }
            var la = new LegalityAnalysis(pk);
            if (!la.Valid)
            {
                string responseMessage;
                if (pk.IsEgg)
                {
                    string speciesName = GameInfo.GetStrings("en").specieslist[pk.Species];
                    responseMessage = $"Invalid Showdown Set for the {speciesName} egg. Please review your information and try again.";
                }
                else
                {
                    responseMessage = $"{typeof(T).Name} attachment is not legal, and cannot be traded!";
                }
                var reply = await ReplyAsync(responseMessage).ConfigureAwait(false);
                await Task.Delay(6000);
                await reply.DeleteAsync().ConfigureAwait(false);
                return false;
            }

            await QueueHelper<T>.AddToQueueAsync(Context, code, trainerName, sig, pk, PokeRoutineType.LinkTrade, tradeType, usr, isBatchTrade, batchTradeNumber, totalBatchTrades, isMysteryEgg, lgcode, ignoreAutoOT, isHiddenTrade).ConfigureAwait(false);
            return true;
        }

        private static List<Pictocodes> GenerateRandomPictocodes(int count)
        {
            Random rnd = new();
            List<Pictocodes> randomPictocodes = new List<Pictocodes>();
            Array pictocodeValues = Enum.GetValues(typeof(Pictocodes));

            for (int i = 0; i < count; i++)
            {
                Pictocodes randomPictocode = (Pictocodes)pictocodeValues.GetValue(rnd.Next(pictocodeValues.Length));
                randomPictocodes.Add(randomPictocode);
            }

            return randomPictocodes;
        }
    }
}
