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
    /// <summary>
    /// Module for generating and trading random eggs with perfect IVs and shiny status.
    /// </summary>
    /// <typeparam name="T">Type of Pokémon to generate</typeparam>
    public class MysteryEggModule<T> : ModuleBase<SocketCommandContext> where T : PKM, new()
    {
        private static TradeQueueInfo<T> Info => SysCord<T>.Runner.Hub.Queues.Info;
        private static readonly Random Random = new();
        private static readonly Dictionary<GameVersion, Queue<ushort>> ShuffledSpeciesDecks = new();

        /// <summary>
        /// Command to generate and trade a mystery egg to the user.
        /// </summary>
        /// <returns>Asynchronous task</returns>
        [Command("mysteryegg")]
        [Alias("me")]
        [Summary("Trades an egg generated from a random Pokémon.")]
        public async Task TradeMysteryEggAsync()
        {
            var userID = Context.User.Id;
            if (Info.IsUserInQueue(userID))
            {
                await ReplyAsync("You already have an existing trade in the queue. Please wait until it is processed.").ConfigureAwait(false);
                return;
            }
            var code = Info.GetRandomTradeCode(Context.User.Id, Context.Channel, Context.User);

            _ = Task.Run(async () =>
            {
                try
                {
                    await ProcessMysteryEggTradeAsync(code).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    LogUtil.LogSafe(ex, nameof(MysteryEggModule<T>));
                    await ReplyAsync("An error occurred while processing the request.").ConfigureAwait(false);
                }
            });
        }

        /// <summary>
        /// Generates a legal egg from a random breedable Pokémon.
        /// </summary>
        /// <param name="maxAttempts">Maximum number of attempts to generate a legal egg</param>
        /// <returns>A legal egg Pokémon, or null if generation fails</returns>
        public static T? GenerateLegalMysteryEgg(int maxAttempts = 10)
        {
            var gameVersion = GetGameVersion();

            if (gameVersion == GameVersion.PLA)
                return null;

            // Initialize the species deck if needed
            if (!ShuffledSpeciesDecks.TryGetValue(gameVersion, out var deck) || deck.Count == 0)
            {
                InitializeSpeciesDeck(gameVersion);
            }

            for (int attempt = 0; attempt < maxAttempts; attempt++)
            {
                // Get next species from our shuffled deck
                ushort speciesId = GetNextSpeciesFromDeck(gameVersion);
                var speciesName = GameInfo.GetStrings("en").specieslist[speciesId];

                var showdownSet = new ShowdownSet(speciesName);
                var template = AutoLegalityWrapper.GetTemplate(showdownSet);
                var sav = AutoLegalityWrapper.GetTrainerInfo<T>();
                var pk = sav.GetLegal(template, out _);

                if (pk == null)
                    continue;

                pk = EntityConverter.ConvertToType(pk, typeof(T), out _) ?? pk;
                if (pk is not T validPk)
                    continue;

                AbstractTrade<T>.EggTrade(validPk, template);
                SetHaX(validPk);

                var la = new LegalityAnalysis(validPk);
                if (la.Valid)
                    return validPk;
            }

            return null;
        }

        /// <summary>
        /// Initializes the species deck for a game version using EggEncounter classes to validate breedable species.
        /// </summary>
        /// <param name="gameVersion">Game version to initialize the deck for</param>
        private static void InitializeSpeciesDeck(GameVersion gameVersion)
        {
            var breedableSpecies = new List<ushort>();

            // Get max species ID based on game version
            ushort maxSpecies = gameVersion switch
            {
                GameVersion.SV => (ushort)PersonalTable.SV.MaxSpeciesID,
                GameVersion.SWSH => (ushort)PersonalTable.SWSH.MaxSpeciesID,
                GameVersion.BDSP => (ushort)PersonalTable.BDSP.MaxSpeciesID,
                _ => (ushort)1010, // Reasonable default
            };

            // Check each species using the appropriate EggEncounter class
            for (ushort species = 1; species <= maxSpecies; species++)
            {
                try
                {
                    IEncounterEgg? encounter = gameVersion switch
                    {
                        GameVersion.BDSP => new EncounterEgg8b(species, 0, gameVersion),
                        GameVersion.SWSH => new EncounterEgg8(species, 0, gameVersion),
                        GameVersion.SV => new EncounterEgg9(species, 0, gameVersion),
                        _ => null
                    };

                    if (encounter != null)
                    {
                        // Check if this is a base form (we only want base forms for mystery eggs)
                        var pi = GetPersonalInfo(gameVersion, species);
                        if (pi != null && pi.EvoStage == 1)
                        {
                            breedableSpecies.Add(species);
                        }
                    }
                }
                catch
                {
                    // Skip species that throw exceptions during encounter creation
                }
            }

            // Shuffle and store the deck
            var shuffled = breedableSpecies.OrderBy(_ => Random.Next()).ToList();
            ShuffledSpeciesDecks[gameVersion] = new Queue<ushort>(shuffled);
        }

        /// <summary>
        /// Gets personal info for a species in a specific game version.
        /// </summary>
        /// <param name="gameVersion">Game version</param>
        /// <param name="species">Species ID</param>
        /// <returns>PersonalInfo object or null if not found</returns>
        private static PersonalInfo? GetPersonalInfo(GameVersion gameVersion, ushort species)
        {
            try
            {
                return gameVersion switch
                {
                    GameVersion.SV => PersonalTable.SV.GetFormEntry(species, 0),
                    GameVersion.SWSH => PersonalTable.SWSH.GetFormEntry(species, 0),
                    GameVersion.BDSP => PersonalTable.BDSP.GetFormEntry(species, 0),
                    _ => null
                };
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Gets the next species from the shuffled deck for the specified game version.
        /// </summary>
        /// <param name="gameVersion">Game version to get species for</param>
        /// <returns>Species ID to use for egg generation</returns>
        private static ushort GetNextSpeciesFromDeck(GameVersion gameVersion)
        {
            if (!ShuffledSpeciesDecks.TryGetValue(gameVersion, out var deck) || deck.Count == 0)
            {
                InitializeSpeciesDeck(gameVersion);
                deck = ShuffledSpeciesDecks[gameVersion];
            }

            return deck.Dequeue();
        }

        /// <summary>
        /// Processes the mystery egg trade request.
        /// </summary>
        /// <param name="code">Link trade code</param>
        /// <returns>Asynchronous task</returns>
        private async Task ProcessMysteryEggTradeAsync(int code)
        {
            var mysteryEgg = GenerateLegalMysteryEgg(10);
            if (mysteryEgg == null)
            {
                await ReplyAsync("Failed to generate a legal mystery egg. Please try again later.").ConfigureAwait(false);
                return;
            }

            var sig = Context.User.GetFavor();
            await AddTradeToQueueAsync(code, Context.User.Username, mysteryEgg, sig, Context.User, isMysteryEgg: true).ConfigureAwait(false);

            if (Context.Message is IUserMessage userMessage)
            {
                await DeleteMessageAfterDelay(userMessage, 2000).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Deletes a message after a specified delay.
        /// </summary>
        /// <param name="message">Message to delete</param>
        /// <param name="delayMilliseconds">Delay in milliseconds</param>
        /// <returns>Asynchronous task</returns>
        private static async Task DeleteMessageAfterDelay(IUserMessage message, int delayMilliseconds)
        {
            await Task.Delay(delayMilliseconds).ConfigureAwait(false);
            await message.DeleteAsync().ConfigureAwait(false);
        }

        /// <summary>
        /// Sets HaX properties on a Pokémon (perfect IVs, shiny, etc.).
        /// </summary>
        /// <param name="pk">Pokémon to modify</param>
        public static void SetHaX(PKM pk)
        {
            pk.IVs = [31, 31, 31, 31, 31, 31];
            pk.SetShiny();
            pk.RefreshAbility(2);
            pk.MaximizeFriendship();
            pk.RefreshChecksum();
        }

        /// <summary>
        /// Gets the appropriate game version based on the generic type parameter.
        /// </summary>
        /// <returns>Game version for the current type</returns>
        public static GameVersion GetGameVersion()
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

        /// <summary>
        /// Adds a trade to the queue.
        /// </summary>
        /// <param name="code">Link trade code</param>
        /// <param name="trainerName">Trainer name</param>
        /// <param name="pk">Pokémon to trade</param>
        /// <param name="sig">Request significance</param>
        /// <param name="usr">User requesting the trade</param>
        /// <param name="isBatchTrade">Whether this is part of a batch trade</param>
        /// <param name="batchTradeNumber">Batch trade number</param>
        /// <param name="totalBatchTrades">Total batch trades</param>
        /// <param name="isHiddenTrade">Whether this is a hidden trade</param>
        /// <param name="isMysteryEgg">Whether this is a mystery egg trade</param>
        /// <param name="lgcode">List of picture codes for Let's Go trades</param>
        /// <param name="tradeType">Type of trade</param>
        /// <param name="ignoreAutoOT">Whether to ignore auto OT</param>
        /// <returns>Asynchronous task</returns>
        private async Task AddTradeToQueueAsync(
           int code,
           string trainerName,
           T pk,
           RequestSignificance sig,
           SocketUser usr,
           bool isBatchTrade = false,
           int batchTradeNumber = 1,
           int totalBatchTrades = 1,
           bool isHiddenTrade = false,
           bool isMysteryEgg = false,
           List<Pictocodes>? lgcode = null,
           PokeTradeType tradeType = PokeTradeType.Specific,
           bool ignoreAutoOT = false)
        {
            lgcode ??= GenerateRandomPictocodes(3);

            var la = new LegalityAnalysis(pk);
            if (!la.Valid)
            {
                string responseMessage = "An unexpected error occurred. Please try again.";
                var reply = await ReplyAsync(responseMessage).ConfigureAwait(false);
                await Task.Delay(6000).ConfigureAwait(false);
                await reply.DeleteAsync().ConfigureAwait(false);
                return;
            }

            await QueueHelper<T>.AddToQueueAsync(
                Context,
                code,
                trainerName,
                sig,
                pk,
                PokeRoutineType.LinkTrade,
                tradeType,
                usr,
                isBatchTrade,
                batchTradeNumber,
                totalBatchTrades,
                isHiddenTrade,
                isMysteryEgg,
                lgcode: lgcode,
                ignoreAutoOT: ignoreAutoOT).ConfigureAwait(false);
        }

        /// <summary>
        /// Generates a random list of picture codes for Let's Go trades.
        /// </summary>
        /// <param name="count">Number of picture codes to generate</param>
        /// <returns>List of random picture codes</returns>
        private static List<Pictocodes> GenerateRandomPictocodes(int count)
        {
            List<Pictocodes> randomPictocodes = new();
            Array pictocodeValues = Enum.GetValues<Pictocodes>();

            for (int i = 0; i < count; i++)
            {
                Pictocodes randomPictocode = (Pictocodes)pictocodeValues.GetValue(Random.Next(pictocodeValues.Length));
                randomPictocodes.Add(randomPictocode);
            }

            return randomPictocodes;
        }
    }
}
