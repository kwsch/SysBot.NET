using Discord;
using Discord.Commands;
using Discord.Net;
using Discord.WebSocket;
using PKHeX.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using DiscordColor = Discord.Color;

namespace SysBot.Pokemon.Discord
{
    /// <summary>
    /// Provides functionality for listing and requesting Pokémon wondercard events via Discord commands.
    /// Users can interact with the system in multiple ways:
    /// 
    /// 1. Listing Events: 
    ///    - Users can list events from a specified generation or game. Optionally, users can filter this list by specifying a Pokémon species name.
    ///    - Command format: .srp {generationOrGame} [speciesName] [pageX]
    ///    - Example: .srp gen9 Mew page2
    ///      This command lists the second page of events for the 'gen9' dataset, specifically filtering for events related to 'Mew'.
    ///
    /// 2. Requesting Specific Events:
    ///    - Users can request a specific event to be processed by providing an event index number.
    ///    - Command format: .srp {generationOrGame} {eventIndex}
    ///    - Example: .srp gen9 26
    ///      This command requests the processing of the event at index 26 within the 'gen9' dataset.
    ///
    /// 3. Pagination:
    ///    - Users can navigate through pages of event listings by specifying the page number after the generation/game and optionally the species.
    ///    - Command format: .srp {generationOrGame} [speciesName] pageX
    ///    - Example: .srp gen9 page3
    ///      This command lists the third page of events for the 'gen9' dataset.
    ///
    /// This module ensures that user inputs are properly validated for the specific commands to manage event data effectively, adjusting listings or processing requests based on user interactions.
    /// </summary>
    public class SpecialRequestModule<T> : ModuleBase<SocketCommandContext> where T : PKM, new()
    {
        private const int itemsPerPage = 25;
        private static TradeQueueInfo<T> Info => SysCord<T>.Runner.Hub.Queues.Info;

        private static T? GetRequest(Download<PKM> dl)
        {
            if (!dl.Success)
                return null;
            return dl.Data switch
            {
                null => null,
                T pk => pk,
                _ => EntityConverter.ConvertToType(dl.Data, typeof(T), out _) as T,
            };
        }

        [Command("specialrequestpokemon")]
        [Alias("srp")]
        [Summary("Lists available wondercard events from the specified generation or game or requests a specific event if a number is provided.")]
        public async Task ListSpecialEventsAsync(string generationOrGame, [Remainder] string args = "")
        {
            var botPrefix = SysCord<T>.Runner.Config.Discord.CommandPrefix;
            var parts = args.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length == 1 && int.TryParse(parts[0], out int index))
            {
                await SpecialEventRequestAsync(generationOrGame, index.ToString());
                return;
            }

            int page = 1;
            string speciesName = "";

            foreach (string part in parts)
            {
                if (part.StartsWith("page", StringComparison.OrdinalIgnoreCase) && int.TryParse(part.Substring(4), out int pageNumber))
                {
                    page = pageNumber;
                    continue;
                }
                speciesName = part;
            }

            var eventData = GetEventData(generationOrGame);
            if (eventData == null)
            {
                await ReplyAsync($"Invalid generation or game: {generationOrGame}");
                return;
            }

            var allEvents = GetFilteredEvents(eventData, speciesName);
            if (!allEvents.Any())
            {
                await ReplyAsync($"No events found for {generationOrGame} with the specified filter.");
                return;
            }

            var pageCount = (int)Math.Ceiling((double)allEvents.Count() / itemsPerPage);
            page = Math.Clamp(page, 1, pageCount);
            var embed = BuildEventListEmbed(generationOrGame, allEvents, page, pageCount, botPrefix);
            await SendEventListAsync(embed);
            await CleanupMessagesAsync();
        }

        [Command("specialrequestpokemon")]
        [Alias("srp")]
        [Summary("Downloads wondercard event attachments from the specified generation and adds to trade queue.")]
        [RequireQueueRole(nameof(DiscordManager.RolesTrade))]
        public async Task SpecialEventRequestAsync(string generationOrGame, [Remainder] string args = "")
        {
            if (!int.TryParse(args, out int index))
            {
                await ReplyAsync("Invalid event index. Please provide a valid event number.");
                return;
            }

            var userID = Context.User.Id;
            if (Info.IsUserInQueue(userID))
            {
                await ReplyAsync("You already have an existing trade in the queue. Please wait until it is processed.");
                return;
            }

            try
            {
                var eventData = GetEventData(generationOrGame);
                if (eventData == null)
                {
                    await ReplyAsync($"Invalid generation or game: {generationOrGame}");
                    return;
                }

                var entityEvents = eventData.Where(gift => gift.IsEntity && !gift.IsItem).ToArray();
                if (index < 1 || index > entityEvents.Length)
                {
                    await ReplyAsync($"Invalid event index. Please use a valid event number from the `{SysCord<T>.Runner.Config.Discord.CommandPrefix}srp {generationOrGame}` command.");
                    return;
                }

                var selectedEvent = entityEvents[index - 1];
                var pk = ConvertEventToPKM(selectedEvent);
                if (pk == null)
                {
                    await ReplyAsync("Wondercard data provided is not compatible with this module!");
                    return;
                }

                var code = Info.GetRandomTradeCode(userID);
                var lgcode = Info.GetRandomLGTradeCode();
                var sig = Context.User.GetFavor();
                await ReplyAsync("Special event request added to queue.");
                await AddTradeToQueueAsync(code, Context.User.Username, pk as T, sig, Context.User, lgcode: lgcode);
            }
            catch (Exception ex)
            {
                await ReplyAsync($"An error occurred: {ex.Message}");
            }
            finally
            {
                await CleanupUserMessageAsync();
            }
        }

        private static int GetPageNumber(string[] parts)
        {
            var pagePart = parts.FirstOrDefault(p => p.StartsWith("page", StringComparison.OrdinalIgnoreCase));
            if (pagePart != null && int.TryParse(pagePart.AsSpan(4), out int pageNumber))
                return pageNumber;

            if (parts.Length > 0 && int.TryParse(parts.Last(), out int parsedPage))
                return parsedPage;

            return 1;
        }

        private static MysteryGift[]? GetEventData(string generationOrGame)
        {
            return generationOrGame.ToLowerInvariant() switch
            {
                "3" or "gen3" => EncounterEvent.MGDB_G3,
                "4" or "gen4" => EncounterEvent.MGDB_G4,
                "5" or "gen5" => EncounterEvent.MGDB_G5,
                "6" or "gen6" => EncounterEvent.MGDB_G6,
                "7" or "gen7" => EncounterEvent.MGDB_G7,
                "gg" or "lgpe" => EncounterEvent.MGDB_G7GG,
                "swsh" => EncounterEvent.MGDB_G8,
                "pla" or "la" => EncounterEvent.MGDB_G8A,
                "bdsp" => EncounterEvent.MGDB_G8B,
                "9" or "gen9" => EncounterEvent.MGDB_G9,
                _ => null,
            };
        }

        private static IOrderedEnumerable<(int Index, string EventInfo)> GetFilteredEvents(MysteryGift[] eventData, string speciesName = "")
        {
            return eventData
                .Where(gift =>
                    gift.IsEntity &&
                    !gift.IsItem &&
                    (string.IsNullOrWhiteSpace(speciesName) || GameInfo.Strings.Species[gift.Species].Equals(speciesName, StringComparison.OrdinalIgnoreCase))
                )
                .Select((gift, index) =>
                {
                    string species = GameInfo.Strings.Species[gift.Species];
                    string levelInfo = $"(Lv. {gift.Level})";
                    string formName = ShowdownParsing.GetStringFromForm(gift.Form, GameInfo.Strings, gift.Species, gift.Context);
                    formName = !string.IsNullOrEmpty(formName) ? $"-{formName}" : formName;
                    return (Index: index + 1, EventInfo: $"{gift.CardHeader} - {species}{formName} {levelInfo}");
                })
                .OrderBy(x => x.Index);
        }

        private static EmbedBuilder BuildEventListEmbed(string generationOrGame, IOrderedEnumerable<(int Index, string EventInfo)> allEvents, int page, int pageCount, string botPrefix)
        {
            var embed = new EmbedBuilder()
                .WithTitle($"Available Events - {generationOrGame.ToUpperInvariant()}")
                .WithDescription($"Page {page} of {pageCount}")
                .WithColor(DiscordColor.Blue);

            var pageItems = allEvents.Skip((page - 1) * itemsPerPage).Take(itemsPerPage);
            foreach (var item in pageItems)
            {
                embed.AddField($"{item.Index}. {item.EventInfo}", $"Use `{botPrefix}srp {generationOrGame} {item.Index}` to request this event.");
            }

            return embed;
        }

        private async Task SendEventListAsync(EmbedBuilder embed)
        {
            if (Context.User is not IUser user)
            {
                await ReplyAsync("**Error**: Unable to send a DM. Please check your **Server Privacy Settings**.");
                return;
            }

            try
            {
                var dmChannel = await user.CreateDMChannelAsync();
                await dmChannel.SendMessageAsync(embed: embed.Build());
                await ReplyAsync($"{Context.User.Mention}, I've sent you a DM with the list of events.");
            }
            catch (HttpException ex) when (ex.HttpCode == HttpStatusCode.Forbidden)
            {
                await ReplyAsync($"{Context.User.Mention}, I'm unable to send you a DM. Please check your **Server Privacy Settings**.");
            }
        }

        private async Task CleanupMessagesAsync()
        {
            await Task.Delay(10_000);
            await CleanupUserMessageAsync();
        }

        private async Task CleanupUserMessageAsync()
        {
            if (Context.Message is IUserMessage userMessage)
                await userMessage.DeleteAsync().ConfigureAwait(false);
        }

        private static PKM? ConvertEventToPKM(MysteryGift selectedEvent)
        {
            var download = new Download<PKM>
            {
                Data = selectedEvent.ConvertToPKM(new SimpleTrainerInfo(), EncounterCriteria.Unrestricted),
                Success = true
            };

            return download.Data is null ? null : GetRequest(download);
        }

        private async Task AddTradeToQueueAsync(int code, string trainerName, T? pk, RequestSignificance sig, SocketUser usr, bool isBatchTrade = false, int batchTradeNumber = 1, int totalBatchTrades = 1, bool isMysteryEgg = false, List<Pictocodes> lgcode = null, PokeTradeType tradeType = PokeTradeType.Specific, bool ignoreAutoOT = false, bool isHiddenTrade = false)
        {
            lgcode ??= GenerateRandomPictocodes(3);
            if (!pk.CanBeTraded())
            {
                var reply = await ReplyAsync("Provided Pokémon content is blocked from trading!").ConfigureAwait(false);
                await Task.Delay(6000); // Delay for 6 seconds
                await reply.DeleteAsync().ConfigureAwait(false);
                return;
            }
            var homeLegalityCfg = Info.Hub.Config.Trade.HomeLegalitySettings;
            var la = new LegalityAnalysis(pk);
            if (!la.Valid)
            {
                string responseMessage = pk.IsEgg ? "Invalid Showdown Set for this Egg. Please review your information and try again." :
                    $"{typeof(T).Name} attachment is not legal, and cannot be traded!";
                var reply = await ReplyAsync(responseMessage).ConfigureAwait(false);
                await Task.Delay(6000);
                await reply.DeleteAsync().ConfigureAwait(false);
                return;
            }
            if (homeLegalityCfg.DisallowNonNatives && (la.EncounterOriginal.Context != pk.Context || pk.GO))
            {
                // Allow the owner to prevent trading entities that require a HOME Tracker even if the file has one already.
                await ReplyAsync($"{typeof(T).Name} attachment is not native, and cannot be traded!").ConfigureAwait(false);
                return;
            }
            if (homeLegalityCfg.DisallowTracked && pk is IHomeTrack { HasTracker: true })
            {
                // Allow the owner to prevent trading entities that already have a HOME Tracker.
                await ReplyAsync($"{typeof(T).Name} attachment is tracked by HOME, and cannot be traded!").ConfigureAwait(false);
                return;
            }
            // handle past gen file requests
            // thanks manu https://github.com/Manu098vm/SysBot.NET/commit/d8c4b65b94f0300096704390cce998940413cc0d
            if (!la.Valid && la.Results.Any(m => m.Identifier is CheckIdentifier.Memory))
            {
                var clone = (T)pk.Clone();

                clone.HandlingTrainerName = pk.OriginalTrainerName;
                clone.HandlingTrainerGender = pk.OriginalTrainerGender;

                if (clone is PK8 or PA8 or PB8 or PK9)
                    ((dynamic)clone).HandlingTrainerLanguage = (byte)pk.Language;

                clone.CurrentHandler = 1;

                la = new LegalityAnalysis(clone);

                if (la.Valid) pk = clone;
            }

            await QueueHelper<T>.AddToQueueAsync(Context, code, trainerName, sig, pk, PokeRoutineType.LinkTrade, tradeType, usr, isBatchTrade, batchTradeNumber, totalBatchTrades, isMysteryEgg, lgcode, ignoreAutoOT, isHiddenTrade).ConfigureAwait(false);
        }

        private static List<Pictocodes> GenerateRandomPictocodes(int count)
        {
            Random rnd = new();
            List<Pictocodes> randomPictocodes = [];
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

