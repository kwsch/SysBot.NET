using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using PKHeX.Core;

namespace SysBot.Pokemon.Discord
{
    [Summary("Clears and toggles Queue features.")]
    public class QueueModule : ModuleBase<SocketCommandContext>
    {
        internal static TradeQueueInfo<PK8> Info => SysCordInstance.Self.Hub.Queues.Info;

        [Command("queueStatus")]
        [Alias("qs", "ts")]
        [Summary("Prints the user's status in the trade queues.")]
        public async Task GetTradePositionAsync()
        {
            var msg = Info.GetPositionString(Context.User.Id);
            await ReplyAsync(msg).ConfigureAwait(false);
        }

        [Command("queueClear")]
        [Alias("qc", "tc")]
        [Summary("Clears the user from the trade queues. Will not remove a user if they are being processed.")]
        public async Task ClearTradeAsync()
        {
            string msg = ClearTrade();
            await ReplyAsync(msg).ConfigureAwait(false);
        }

        [Command("queueClearAll")]
        [Alias("qca", "tca")]
        [Summary("Clears all users from the trade queues.")]
        public async Task ClearAllTradesAsync()
        {
            if (!Context.GetIsSudo(Info.Hub.Config))
            {
                await ReplyAsync("You can't use this command.").ConfigureAwait(false);
                return;
            }

            Info.ClearAllQueues();
            await ReplyAsync("Cleared all in the queue.").ConfigureAwait(false);
        }

        [Command("queueToggle")]
        [Alias("qt", "tt")]
        [Summary("Toggles on/off the ability to join the trade queue.")]
        public async Task ToggleQueueTradeAsync()
        {
            if (!Context.GetIsSudo(Info.Hub.Config))
            {
                await ReplyAsync("You can't use this command.").ConfigureAwait(false);
                return;
            }

            Info.CanQueue ^= true;
            await ReplyAsync($"CanQueue has been set to: {Info.CanQueue}").ConfigureAwait(false);
        }

        [Command("queueList")]
        [Alias("ql")]
        [Summary("[Sudo Only] Private messages the list of users in the queue.")]
        public async Task ListUserQueue()
        {
            if (!Context.GetIsSudo(Info.Hub.Config))
            {
                await ReplyAsync("You can't use this command.").ConfigureAwait(false);
                return;
            }

            var lines = SysCordInstance.Self.Hub.Queues.Info.GetUserList();
            var msg = string.Join("\n", lines);
            if (msg.Length < 3)
                await ReplyAsync("List is empty.").ConfigureAwait(false);
            else
                await Context.User.SendMessageAsync(msg).ConfigureAwait(false);
        }

        private string ClearTrade()
        {
            var cfg = Info.Hub.Config;
            var sudo = Context.GetIsSudo(cfg);
            var allowed = sudo || (Info.CanQueue && (Context.GetHasRole(cfg.DiscordRoleCanTrade) || Context.GetHasRole(cfg.DiscordRoleCanDudu) || Context.GetHasRole(cfg.DiscordRoleCanClone)));
            if (!allowed)
                return "Sorry, you are not permitted to use this command!";

            var userID = Context.User.Id;
            var result = Info.ClearTrade(userID);
            return result switch
            {
                QueueResultRemove.CurrentlyProcessing => "Looks like you're currently being processed! Unable to remove from queue.",
                QueueResultRemove.Removed => "Removed you from the queue.",
                _ => "Sorry, you are not currently in the queue.",
            };
        }
    }
}