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

        static TradeModule() => AutoLegalityExtensions.EnsureInitialized();

        [Command("tradeStatus")]
        public async Task GetTradePosition()
        {
            string msg;
            lock (_sync)
            {
                var uid = Context.User.Id;
                var index = UsersInQueue.FindIndex(z => z.User == uid);
                msg = index < 0 ? "You are not in the queue." : $"You are in the queue! Position: {index + 1}, Receiving: {(Species)UsersInQueue[index].Trade.TradeData.Species}";
            }
            await ReplyAsync(msg).ConfigureAwait(false);
        }

        [Command("tradeClear")]
        public async Task ClearTradeAsync()
        {
            string msg;
            lock (_sync)
                msg = ClearTrade();
            await ReplyAsync(msg).ConfigureAwait(false);
        }

        [Command("tradeClearAll")]
        public async Task ClearAllTradesAsync()
        {
            var cfg = SysCordInstance.Self.Hub.Config;
            var sudo = Context.GetHasRole(cfg.DiscordRoleSudo);
            if (!sudo)
            {
                await ReplyAsync("You can't use this command.").ConfigureAwait(false);
                return;
            }

            lock (_sync)
            {
                SysCordInstance.Self.Hub.Queue.Clear();
                UsersInQueue.Clear();
            }
            await ReplyAsync("Cleared all in the queue.").ConfigureAwait(false);
        }

        [Command("trade")]
        [Summary("Makes the bot trade you the provided PKM by adding it to the pool.")]
        public async Task TradeAsync([Summary("Trade Code")]int code, [Remainder][Summary("Trainer Name to trade to.")]string trainerName)
        {
            var cfg = SysCordInstance.Self.Hub.Config;
            var sudo = Context.GetHasRole(cfg.DiscordRoleSudo);
            var allowed = sudo || Context.GetHasRole(cfg.DiscordRoleCanTrade);
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
        [Summary("Makes the bot trade you the provided Showdown Set by adding it to the pool.")]
        public async Task TradeAsync([Summary("Showdown Set")][Remainder]string content)
        {
            var cfg = SysCordInstance.Self.Hub.Config;
            var sudo = Context.GetHasRole(cfg.DiscordRoleSudo);
            var allowed = sudo || Context.GetHasRole(cfg.DiscordRoleCanTrade);
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

            var code = Util.Rand.Next(0, 9999);
            await AddTradeToQueue(code, Context.User.Username, pk8, sudo).ConfigureAwait(false);
        }

        [Command("seedcheck")]
        [Summary("Checks the seed for a pokemon.")]
        public async Task SeedCheckAsync()
        {
            var cfg = SysCordInstance.Self.Hub.Config;
            var sudo = Context.GetHasRole(cfg.DiscordRoleSudo);
            var allowed = sudo || Context.GetHasRole(cfg.DiscordRoleCanTrade);
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
            var cfg = SysCordInstance.Self.Hub.Config;
            var sudo = Context.GetHasRole(cfg.DiscordRoleSudo);
            var allowed = sudo || Context.GetHasRole(cfg.DiscordRoleCanTrade);
            if (!allowed)
                return "Sorry, you are not permitted to use this command!";

            var userID = Context.User.Id;
            var details = UsersInQueue.Where(z => z.User == userID).ToArray();
            if (details.Length == 0)
                return "Sorry, you are not currently in the queue.";

            int removedCount = 0;
            foreach (var detail in details)
            {
                int removed = SysCordInstance.Self.Hub.Queue.Remove(detail.Trade);
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

                var hub = SysCordInstance.Self.Hub;
                var queue = type switch
                {
                    PokeRoutineType.DuduBot => hub.Dudu,
                    _ => hub.Queue,
                };
                queue.Enqueue(detail, priority);

                var trade = new TradeEntry<PK8>(detail, userID);
                UsersInQueue.Add(trade);
                notifier.OnFinish = () =>
                {
                    lock (_sync)
                        UsersInQueue.Remove(trade);
                };
            }

            msg = $"Added {Context.User.Mention} to the queue. Your current position is: {SysCordInstance.Self.Hub.Queue.Count}";
            return true;
        }

        [Command("trade")]
        public async Task TradeAsync()
        {
            await TradeAsync(Util.Rand.Next(0, 9999), string.Empty).ConfigureAwait(false);
        }
    }
}