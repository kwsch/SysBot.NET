using Discord;
using Discord.Commands;
using PKHeX.Core;
using PKHeX.Core.AutoMod;
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
        private static readonly Dictionary<EntityContext, List<ushort>> BreedableSpeciesCache = [];
        private const int DefaultMaxGenerationAttempts = 30;

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

            var code = Info.GetRandomTradeCode((int)userID);
            _ = Task.Run(async () =>
            {
                try
                {
                    await ProcessMysteryEggTradeAsync(code).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    LogUtil.LogSafe(ex, nameof(MysteryEggModule<T>));
                }
            });
        }

        /// <summary>
        /// Generates a legal mystery egg with shiny status, perfect IVs, and hidden ability if available.
        /// </summary>
        /// <param name="maxAttempts">Maximum number of species to try before giving up</param>
        /// <returns>A legal egg Pokemon, or null if generation failed</returns>
        public static T? GenerateLegalMysteryEgg(int maxAttempts = DefaultMaxGenerationAttempts)
        {
            // Generate eggs with desired attributes (shiny, 6IV, HA) by requesting them in the ShowdownSet
            // This ensures proper correlation for BDSP and clean generation for all games

            var context = GetContext();
            if (context == EntityContext.None)
                return null;

            var breedableSpecies = GetBreedableSpecies(context);
            if (breedableSpecies.Count == 0)
                return null;

            var random = new Random();
            var shuffled = breedableSpecies.OrderBy(_ => random.Next()).Take(maxAttempts).ToList();

            var sav = AutoLegalityWrapper.GetTrainerInfo<T>();

            // Temporarily set game priority to ensure eggs generate for the correct game
            var originalPriority = APILegality.PriorityOrder?.ToList() ?? [];
            APILegality.PriorityOrder = GetPriorityOrder();

            try
            {
                foreach (var species in shuffled)
                {
                    var set = CreateEggShowdownSet(species, context);
                    var template = AutoLegalityWrapper.GetTemplate(set);
                    var pk = sav.GetLegal(template, out _);

                    if (pk == null)
                        continue;

                    pk = EntityConverter.ConvertToType(pk, typeof(T), out _) ?? pk;
                    if (pk is not T validPk)
                        continue;
                    AbstractTrade<T>.EggTrade(validPk, template);

                    var la = new LegalityAnalysis(validPk);
                    if (la.Valid)
                        return validPk;
                }
            }
            finally
            {
                APILegality.PriorityOrder = originalPriority;
            }

            return null;
        }

        private static ShowdownSet CreateEggShowdownSet(ushort species, EntityContext context)
        {
            var speciesName = GameInfo.Strings.Species[species];
            var setString = $"{speciesName}\nShiny: Yes\nIVs: 31/31/31/31/31/31";

            // Try to add hidden ability if available
            var hiddenAbilityName = GetHiddenAbilityName(species, context);
            if (!string.IsNullOrEmpty(hiddenAbilityName))
                setString += $"\nAbility: {hiddenAbilityName}";

            return new ShowdownSet(setString);
        }

        private static string? GetHiddenAbilityName(ushort species, EntityContext context)
        {
            // First check PKHeX's breed legality
            if (!AbilityBreedLegality.IsHiddenPossibleHOME(species))
                return null;

            var personalTable = GetPersonalTable(context);
            if (personalTable == null)
                return null;

            try
            {
                var pi = personalTable.GetFormEntry(species, 0);
                if (pi is IPersonalAbility12H piH)
                {
                    var hiddenAbilityID = piH.AbilityH;
                    if (hiddenAbilityID > 0 && hiddenAbilityID < GameInfo.Strings.Ability.Count)
                        return GameInfo.Strings.Ability[hiddenAbilityID];
                }
            }
            catch (Exception ex)
            {
                LogUtil.LogSafe(ex, $"Failed to get hidden ability for species {species}");
            }

            return null;
        }

        private static List<ushort> GetBreedableSpecies(EntityContext context)
        {
            lock (BreedableSpeciesCache)
            {
                if (BreedableSpeciesCache.TryGetValue(context, out var cached))
                    return cached;
            }

            var personalTable = GetPersonalTable(context);
            if (personalTable == null)
                return [];

            var breedable = new List<ushort>();

            for (ushort species = 1; species <= personalTable.MaxSpeciesID; species++)
            {
                if (!Breeding.CanHatchAsEgg(species))
                    continue;

                if (!personalTable.IsSpeciesInGame(species))
                    continue;

                breedable.Add(species);
            }

            lock (BreedableSpeciesCache)
            {
                BreedableSpeciesCache[context] = breedable;
            }

            return breedable;
        }

        private static EntityContext GetContext() => typeof(T).Name switch
        {
            "PB8" => EntityContext.Gen8b,
            "PK8" => EntityContext.Gen8,
            "PK9" => EntityContext.Gen9,
            _ => EntityContext.None
        };

        private static List<GameVersion> GetPriorityOrder() => GetContext() switch
        {
            EntityContext.Gen8b => [GameVersion.BD, GameVersion.SP],
            EntityContext.Gen8 => [GameVersion.SW, GameVersion.SH],
            EntityContext.Gen9 => [GameVersion.SL, GameVersion.VL],
            _ => [] // Return empty list for unsupported contexts
        };

        private static IPersonalTable? GetPersonalTable(EntityContext context) => context switch
        {
            EntityContext.Gen8b => PersonalTable.BDSP,
            EntityContext.Gen8 => PersonalTable.SWSH,
            EntityContext.Gen9 => PersonalTable.SV,
            _ => null
        };

        private async Task ProcessMysteryEggTradeAsync(int code)
        {
            var mysteryEgg = GenerateLegalMysteryEgg();
            if (mysteryEgg == null)
            {
                await ReplyAsync("Failed to generate a legal mystery egg. Please try again later.").ConfigureAwait(false);
                return;
            }

            var sig = Context.User.GetFavor();
            await QueueHelper<T>.AddToQueueAsync(
                Context, code, Context.User.Username, sig, mysteryEgg,
                PokeRoutineType.LinkTrade, PokeTradeType.Specific, Context.User,
                isMysteryEgg: true, lgcode: GenerateRandomPictocodes(3)
            ).ConfigureAwait(false);

            if (Context.Message is IUserMessage userMessage)
                _ = DeleteMessageAfterDelay(userMessage, 2000);
        }

        private static async Task DeleteMessageAfterDelay(IUserMessage message, int delayMilliseconds)
        {
            await Task.Delay(delayMilliseconds).ConfigureAwait(false);
            try
            {
                await message.DeleteAsync().ConfigureAwait(false);
            }
            catch
            {
                // Message may have already been deleted
            }
        }

        private static List<Pictocodes> GenerateRandomPictocodes(int count)
        {
            var random = new Random();
            var values = Enum.GetValues<Pictocodes>();
            var result = new List<Pictocodes>(count);
            for (int i = 0; i < count; i++)
                result.Add(values[random.Next(values.Length)]);
            return result;
        }
    }
}
