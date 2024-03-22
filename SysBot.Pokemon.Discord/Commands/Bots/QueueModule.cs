using Discord;
using Discord.Commands;
using PKHeX.Core;
using System.Threading.Tasks;

namespace SysBot.Pokemon.Discord;

[Summary("Clears and toggles Queue features.")]
public class QueueModule<T> : ModuleBase<SocketCommandContext> where T : PKM, new()
{
    private static TradeQueueInfo<T> Info => SysCord<T>.Runner.Hub.Queues.Info;

    [Command("queueStatus")]
    [Alias("qs", "ts")]
    [Summary("Checks the user's position in the queue.")]
    public async Task GetTradePositionAsync()
    {
        var userID = Context.User.Id;
        var tradeEntry = Info.GetDetail(userID);

        string msg;
        if (tradeEntry != null)
        {
            var uniqueTradeID = tradeEntry.UniqueTradeID;
            msg = Context.User.Mention + " - " + Info.GetPositionString(userID, uniqueTradeID);
        }
        else
        {
            msg = Context.User.Mention + " - You are not currently in the queue.";
        }

        // Send the reply and capture the response message
        var response = await ReplyAsync(msg).ConfigureAwait(false);

        // Delay for 5 seconds
        await Task.Delay(5000).ConfigureAwait(false);

        // Delete user message
        if (Context.Message is IUserMessage userMessage)
            await userMessage.DeleteAsync().ConfigureAwait(false);

        // Delete bot response
        if (response is IUserMessage responseMessage)
            await responseMessage.DeleteAsync().ConfigureAwait(false);
    }

    [Command("queueClear")]
    [Alias("qc", "tc")]
    [Summary("Clears the user from the trade queues. Will not remove a user if they are being processed.")]
    public async Task ClearTradeAsync()
    {
        string msg = ClearTrade();

        // Send the reply and capture the response message
        var response = await ReplyAsync(msg).ConfigureAwait(false);

        // Wait for 5 seconds
        await Task.Delay(5000).ConfigureAwait(false);

        // Delete the user's command message if possible
        if (Context.Message is IUserMessage userMessage)
        {
            await userMessage.DeleteAsync().ConfigureAwait(false);
        }

        // Delete the bot's response message
        await response.DeleteAsync().ConfigureAwait(false);
    }


    [Command("queueClearUser")]
    [Alias("qcu", "tcu")]
    [Summary("Clears the user from the trade queues. Will not remove a user if they are being processed.")]
    [RequireSudo]
    public async Task ClearTradeUserAsync([Summary("Discord user ID")] ulong id)
    {
        string msg = ClearTrade(id);
        await ReplyAsync(msg).ConfigureAwait(false);
    }

    [Command("queueClearUser")]
    [Alias("qcu", "tcu")]
    [Summary("Clears the user from the trade queues. Will not remove a user if they are being processed.")]
    [RequireSudo]
    public async Task ClearTradeUserAsync([Summary("Username of the person to clear")] string _)
    {
        foreach (var user in Context.Message.MentionedUsers)
        {
            string msg = ClearTrade(user.Id);
            await ReplyAsync(msg).ConfigureAwait(false);
        }
    }

    [Command("queueClearUser")]
    [Alias("qcu", "tcu")]
    [Summary("Clears the user from the trade queues. Will not remove a user if they are being processed.")]
    [RequireSudo]
    public async Task ClearTradeUserAsync()
    {
        var users = Context.Message.MentionedUsers;
        if (users.Count == 0)
        {
            await ReplyAsync("No users mentioned").ConfigureAwait(false);
            return;
        }
        foreach (var u in users)
            await ClearTradeUserAsync(u.Id).ConfigureAwait(false);
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
    public Task ToggleQueueTradeAsync()
    {
        var state = Info.ToggleQueue();
        var msg = state
            ? "Users are now able to join the trade queue."
            : "Changed queue settings: **Users CANNOT join the queue until it is turned back on.**";

        return Context.Channel.EchoAndReply(msg);
    }

    [Command("queueMode")]
    [Alias("qm")]
    [Summary("Changes how queueing is controlled (manual/threshold/interval).")]
    [RequireSudo]
    public async Task ChangeQueueModeAsync([Summary("Queue mode")] QueueOpening mode)
    {
        SysCord<T>.Runner.Hub.Config.Queues.QueueToggleMode = mode;
        await ReplyAsync($"Changed queue mode to {mode}.").ConfigureAwait(false);
    }

    [Command("queueList")]
    [Alias("ql")]
    [Summary("Private messages the list of users in the queue.")]
    [RequireSudo]
    public async Task ListUserQueue()
    {
        var lines = SysCord<T>.Runner.Hub.Queues.Info.GetUserList("(ID {0}) - Code: {1} - {2} - {3}");
        var msg = string.Join("\n", lines);
        if (msg.Length < 3)
            await ReplyAsync("Queue list is empty.").ConfigureAwait(false);
        else
            await Context.User.SendMessageAsync(msg).ConfigureAwait(false);
    }

    private string ClearTrade()
    {
        var userID = Context.User.Id;
        return ClearTrade(userID);
    }

    //private static string ClearTrade(string username)
    //{
    //    var result = Info.ClearTrade(username);
    //    return GetClearTradeMessage(result);
    //}

    private static string ClearTrade(ulong userID)
    {
        var userEntries = Info.GetIsUserQueued(entry => entry.UserID == userID);

        if (userEntries.Count == 0)
            return "Sorry, you are not currently in the queue.";

        bool removedAll = true;
        bool currentlyProcessing = false;
        bool removedPending = false;

        foreach (var entry in userEntries)
        {
            if (entry.Trade.IsProcessing)
            {
                currentlyProcessing = true;
                if (!Info.Hub.Config.Queues.CanDequeueIfProcessing)
                {
                    removedAll = false;
                    entry.Trade.IsCanceled = true; // Set the trade as canceled
                    continue;
                }
            }
            else
            {
                entry.Trade.IsCanceled = true; // Set the trade as canceled
                Info.Remove(entry);
                removedPending = true;
            }
        }

        if (!removedAll && currentlyProcessing && !removedPending)
            return "Looks like you have trades currently being processed! Did not remove those from the queue.";

        if (currentlyProcessing && removedPending)
            return "Looks like you have trades currently being processed! Removed other pending trades from the queue.";

        if (removedPending)
            return "Removed your pending trades from the queue.";

        return "Sorry, you are not currently in the queue.";
    }
}
