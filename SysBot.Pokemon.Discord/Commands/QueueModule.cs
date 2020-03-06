using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using PKHeX.Core;

namespace SysBot.Pokemon.Discord
{
    [Summary("Clears and toggles Queue features.")]
    public class QueueModule : ModuleBase<SocketCommandContext>
    {
        private static TradeQueueInfo<PK8> Info => SysCordInstance.Self.Hub.Queues.Info;

        [Command("queueStatus")]
        [Alias("qs", "ts")]
        [Summary("Checks the user's position in the queue.")]
        public async Task GetTradePositionAsync()
        {
            var msg = Context.User.Mention + " - " + Info.GetPositionString(Context.User.Id);
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
            var msg = state
                ? "Users are now able to join the trade queue."
                : "Changed queue settings: **Users CANNOT join the queue until it is turned back on.**";
            await ReplyAsync(msg).ConfigureAwait(false);
        }

        [Command("queueMode")]
        [Alias("qm")]
        [Summary("Toggles on/off the ability to join the trade queue.")]
        [RequireSudo]
        public async Task ChangeQueueModeAsync([Summary("Queue mode")]QueueOpening mode)
        {
            SysCordInstance.Self.Hub.Config.Queues.QueueToggleMode = mode;
            await ReplyAsync($"Changed queue mode to {mode}.").ConfigureAwait(false);
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
                await ReplyAsync("Queue list is empty.").ConfigureAwait(false);
            else
                await Context.User.SendMessageAsync(msg).ConfigureAwait(false);
        }

        private string ClearTrade()
        {
            var userID = Context.User.Id;
            var result = Info.ClearTrade(userID);
            return result switch
            {
                QueueResultRemove.CurrentlyProcessing => "Looks like you're currently being processed! Removed from queue.",
                QueueResultRemove.Removed => "Removed you from the queue.",
                _ => "Sorry, you are not currently in the queue.",
            };
        }
    }
}