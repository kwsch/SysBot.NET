using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using PKHeX.Core;
using PKHeX.Core.AutoMod;

namespace SysBot.Pokemon.Discord
{
    public class TradeModule : ModuleBase<SocketCommandContext>
    {
        private static readonly object _sync = new object();
        private static readonly List<TradeEntry<PK8>> UsersInQueue = new List<TradeEntry<PK8>>();

        private static bool CanQueue = true;

        static TradeModule() => AutoLegalityExtensions.EnsureInitialized();

        [Command("tradeStatus")]
        [Alias("ts")]
        [Summary("Prints the user's status in the trade queues.")]
        public async Task GetTradePosition()
        {
            string msg;
            lock (_sync)
            {
                var uid = Context.User.Id;
                var index = UsersInQueue.FindIndex(z => z.User == uid);
                if (index < 0)
                {
                    msg = "You are not in the queue.";
                }
                else
                {
                    var entry = UsersInQueue[index];
                    var actualIndex = 1;
                    for (int i = 0; i < index; i++)
                    {
                        if (UsersInQueue[i].Type == entry.Type)
                            actualIndex++;
                    }
                    msg = entry.Type == PokeRoutineType.DuduBot
                        ? $"You are in the Dudu queue! Position: {actualIndex}"
                        : $"You are in the Trade queue! Position: {actualIndex}, Receiving: {(Species)entry.Trade.TradeData.Species}";
                }
            }
            await ReplyAsync(msg).ConfigureAwait(false);
        }

        [Command("tradeList")]
        [Alias("tl")]
        [Summary("Prints the users in the trade queues.")]
        public async Task GetTradeList()
        {
            var hub = SysCordInstance.Self.Hub;
            var cfg = hub.Config;
            var sudo = Context.GetHasRole(cfg.DiscordRoleSudo);
            if (!sudo)
            {
                await ReplyAsync("You can't use this command.").ConfigureAwait(false);
                return;
            }

            string msg;
            lock (_sync)
            {
                if (UsersInQueue.Count == 0)
                {
                    msg = "Nobody in any queue.";
                }
                else
                {
                    var queued = UsersInQueue.GroupBy(z => z.Type);
                    var list = queued.SelectMany(z => z.Select(x =>
                        $"{x.Type}: {x.Trade.Trainer.TrainerName} ({x.Name}), {(Species) x.Trade.TradeData.Species}"));
                    msg = string.Join("\n", list);
                }
            }
            await ReplyAsync(msg).ConfigureAwait(false);
        }

        [Command("tradeClear")]
        [Alias("tc")]
        [Summary("Clears the user from the trade queues. Will not remove a user if they are being processed.")]
        public async Task ClearTradeAsync()
        {
            string msg;
            lock (_sync)
                msg = ClearTrade();
            await ReplyAsync(msg).ConfigureAwait(false);
        }

        [Command("tradeClearAll")]
        [Alias("tca")]
        [Summary("Clears all users from the trade queues.")]
        public async Task ClearAllTradesAsync()
        {
            var hub = SysCordInstance.Self.Hub;
            var cfg = hub.Config;
            var sudo = Context.GetHasRole(cfg.DiscordRoleSudo);
            if (!sudo)
            {
                await ReplyAsync("You can't use this command.").ConfigureAwait(false);
                return;
            }

            lock (_sync)
            {
                hub.Queue.Clear();
                hub.Dudu.Clear();
                UsersInQueue.Clear();
            }
            await ReplyAsync("Cleared all in the queue.").ConfigureAwait(false);
        }

        [Command("tradeToggle")]
        [Alias("tt")]
        [Summary("Toggles on/off the ability to join the trade queue.")]
        public async Task ToggleQueueTrade()
        {
            var hub = SysCordInstance.Self.Hub;
            var cfg = hub.Config;
            var sudo = Context.GetHasRole(cfg.DiscordRoleSudo);
            if (!sudo)
            {
                await ReplyAsync("You can't use this command.").ConfigureAwait(false);
                return;
            }

            lock (_sync)
            {
                CanQueue ^= true;
            }
            await ReplyAsync($"CanQueue has been set to: {CanQueue}").ConfigureAwait(false);
        }

        [Command("trade")]
        [Alias("t")]
        [Summary("Makes the bot trade you the provided PKM by adding it to the pool.")]
        public async Task TradeAsync([Summary("Trade Code")]int code, [Remainder][Summary("Trainer Name to trade to.")]string trainerName)
        {
            var cfg = SysCordInstance.Self.Hub.Config;
            var sudo = Context.GetHasRole(cfg.DiscordRoleSudo);
            var allowed = sudo || (Context.GetHasRole(cfg.DiscordRoleCanTrade) && CanQueue);
            if (!allowed)
            {
                await ReplyAsync("Sorry, you are not permitted to use this command!").ConfigureAwait(false);
                return;
            }

            if ((uint)code > 9999)
            {
                await ReplyAsync("Trade code should be 0000-9999!").ConfigureAwait(false);
                return;
            }

            var attachment = Context.Message.Attachments.FirstOrDefault();
            if (attachment == default)
            {
                await ReplyAsync("No attachment provided!").ConfigureAwait(false);
                return;
            }

            var att = await NetUtil.DownloadPKMAsync(attachment).ConfigureAwait(false);
            if (!att.Success || !(att.Data is PK8 pk8))
            {
                await ReplyAsync("No PK8 attachment provided!").ConfigureAwait(false);
                return;
            }

            await AddTradeToQueue(code, trainerName, pk8, sudo).ConfigureAwait(false);
        }

        [Command("trade")]
        [Alias("t")]
        [Summary("Makes the bot trade you the provided Showdown Set by adding it to the pool.")]
        public async Task TradeAsync([Summary("Showdown Set")][Remainder]string content)
        {
            var cfg = SysCordInstance.Self.Hub.Config;
            var sudo = Context.GetHasRole(cfg.DiscordRoleSudo);
            var allowed = sudo || (Context.GetHasRole(cfg.DiscordRoleCanTrade) && CanQueue);
            if (!allowed)
            {
                await ReplyAsync("Sorry, you are not permitted to use this command!").ConfigureAwait(false);
                return;
            }

            const int gen = 8;
            content = ReusableActions.StripCodeBlock(content);
            var set = new ShowdownSet(content);
            var sav = TrainerSettings.GetSavedTrainerData(gen);

            var pkm = sav.GetLegalFromSet(set, out var result);
            var la = new LegalityAnalysis(pkm);
            var spec = GameInfo.Strings.Species[set.Species];
            var msg = la.Valid
                ? $"Here's your ({result}) legalized PKM for {spec}!"
                : $"Oops! I wasn't able to create something from that. Here's my best attempt for that {spec}!";

            if (!la.Valid || !(pkm is PK8 pk8))
            {
                await ReplyAsync(msg).ConfigureAwait(false);
                return;
            }

            pk8.ResetPartyStats();

            var code = Util.Rand.Next(0, 9999);
            await AddTradeToQueue(code, Context.User.Username, pk8, sudo).ConfigureAwait(false);
        }

        [Command("seedcheck")]
        [Alias("dudu", "d")]
        [Summary("Checks the seed for a pokemon.")]
        public async Task SeedCheckAsync()
        {
            var cfg = SysCordInstance.Self.Hub.Config;
            var sudo = Context.GetHasRole(cfg.DiscordRoleSudo);
            var allowed = sudo || (Context.GetHasRole(cfg.DiscordRoleCanDudu) && CanQueue);
            if (!allowed)
            {
                await ReplyAsync("Sorry, you are not permitted to use this command!").ConfigureAwait(false);
                return;
            }

            var code = Util.Rand.Next(0, 9999);
            await AddSeedCheckToQueue(code, Context.User.Username, sudo).ConfigureAwait(false);
        }

        private async Task AddTradeToQueue(int code, string trainerName, PK8 pk8, bool sudo)
        {
            var la = new LegalityAnalysis(pk8);
            if (!la.Valid)
            {
                await ReplyAsync("PK8 attachment is not legal, and cannot be traded!").ConfigureAwait(false);
                return;
            }

            var result = AddToTradeQueue(code, trainerName, sudo, pk8, PokeRoutineType.LinkTrade, out var msg);
            await ReplyAsync(msg).ConfigureAwait(false);
            if (result)
                await Context.Message.DeleteAsync(RequestOptions.Default).ConfigureAwait(false);
        }

        private async Task AddSeedCheckToQueue(int code, string trainer, bool sudo)
        {
            var result = AddToTradeQueue(code, trainer, sudo, new PK8(), PokeRoutineType.DuduBot, out var msg);
            await ReplyAsync(msg).ConfigureAwait(false);
            if (result)
                await Context.Message.DeleteAsync(RequestOptions.Default).ConfigureAwait(false);
        }

        private string ClearTrade()
        {
            var hub = SysCordInstance.Self.Hub;
            var cfg = hub.Config;
            var sudo = Context.GetHasRole(cfg.DiscordRoleSudo);
            var allowed = sudo || (CanQueue && (Context.GetHasRole(cfg.DiscordRoleCanTrade) || Context.GetHasRole(cfg.DiscordRoleCanDudu)));
            if (!allowed)
                return "Sorry, you are not permitted to use this command!";

            var userID = Context.User.Id;
            var details = UsersInQueue.Where(z => z.User == userID).ToArray();
            if (details.Length == 0)
                return "Sorry, you are not currently in the queue.";

            int removedCount = 0;
            foreach (var detail in details)
            {
                int removed = hub.Queue.Remove(detail.Trade);
                if (removed != 0)
                    UsersInQueue.Remove(detail);
                removedCount += removed;

                removed = hub.Dudu.Remove(detail.Trade);
                if (removed != 0)
                    UsersInQueue.Remove(detail);
                removedCount += removed;
            }

            if (removedCount != details.Length)
                return "Looks like you're currently being processed! Unable to remove from queue.";

            return "Removed you from the queue.";
        }

        private bool AddToTradeQueue(int code, string trainerName, bool sudo, PK8 pk8, PokeRoutineType type, out string msg)
        {
            var userID = Context.User.Id;
            var name = Context.User.Username;
            lock (_sync)
            {
                if (UsersInQueue.Any(z => z.User == userID) && !sudo)
                {
                    msg = "Sorry, you are already in the queue.";
                    return false;
                }

                var tmp = new PokeTradeTrainerInfo(trainerName);
                var notifier = new DiscordTradeNotifier<PK8>(pk8, tmp, code, Context);
                var detail = new PokeTradeDetail<PK8>(pk8, tmp, notifier, code: code);
                var priority = sudo ? PokeTradeQueue<PK8>.Tier1 : PokeTradeQueue<PK8>.TierFree;
                var queue = GetHub(type);
                queue.Enqueue(detail, priority);
                msg = $"Added {Context.User.Mention} to the queue. Your current position is: {queue.Count}";

                var trade = new TradeEntry<PK8>(detail, userID, type, name);
                UsersInQueue.Add(trade);
                notifier.OnFinish = () =>
                {
                    lock (_sync)
                        UsersInQueue.Remove(trade);
                };
            }

            return true;
        }

        private static PokeTradeQueue<PK8> GetHub(PokeRoutineType type)
        {
            var hub = SysCordInstance.Self.Hub;
            return type switch
            {
                PokeRoutineType.DuduBot => hub.Dudu,
                _ => hub.Queue,
            };
        }

        [Command("trade")]
        public async Task TradeAsync()
        {
            await TradeAsync(Util.Rand.Next(0, 9999), string.Empty).ConfigureAwait(false);
        }
    }
}