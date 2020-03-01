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
        [RequireSudo]
        public async Task ClearAllTradesAsync()
        {
            Info.ClearAllQueues();
            await ReplyAsync("Cleared all in the queue.").ConfigureAwait(false);
        }

        [Command("queueToggle")]
        [Alias("qt", "tt")]
        [Summary("Toggles on/off the ability to join the trade queue.")]
        [RequireSudo]
        public async Task ToggleQueueTradeAsync()
        {
            var state = Info.ToggleQueue();
            await ReplyAsync($"CanQueue has been set to: {state}").ConfigureAwait(false);
        }

        [Command("queueList")]
        [Alias("ql")]
        [Summary("Private messages the list of users in the queue.")]
        [RequireSudo]
        public async Task ListUserQueue()
        {
            var lines = SysCordInstance.Self.Hub.Queues.Info.GetUserList();
            var msg = string.Join("\n", lines);
            if (msg.Length < 3)
                await ReplyAsync("List is empty.").ConfigureAwait(false);
            else
                await Context.User.SendMessageAsync(msg).ConfigureAwait(false);
        }

        private string ClearTrade()
        {
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