using Discord;
using Discord.Commands;
using PKHeX.Core;
using SysBot.Base;
using System;
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

        await ReplyAndDeleteAsync(msg, 5, Context.Message).ConfigureAwait(false);
    }

    [Command("queueClear")]
    [Alias("qc", "tc")]
    [Summary("Clears the user from the trade queues. Will not remove a user if they are being processed.")]
    public async Task ClearTradeAsync()
    {
        string msg = ClearTrade(Context.User.Id);
        await ReplyAndDeleteAsync(msg, 5, Context.Message).ConfigureAwait(false);
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

    [Command("deleteTradeCode")]
    [Alias("dtc")]
    [Summary("Deletes the stored trade code for the user.")]
    public async Task DeleteTradeCodeAsync()
    {
        var userID = Context.User.Id;
        string msg = QueueModule<T>.DeleteTradeCode(userID);
        await ReplyAsync(msg).ConfigureAwait(false);
    }

    private async Task ReplyAndDeleteAsync(string message, int delaySeconds, IMessage? messageToDelete = null)
    {
        try
        {
            var sentMessage = await ReplyAsync(message).ConfigureAwait(false);
            _ = DeleteMessagesAfterDelayAsync(sentMessage, messageToDelete, delaySeconds);
        }
        catch (Exception ex)
        {
            LogUtil.LogSafe(ex, nameof(QueueModule<T>));
        }
    }

    private async Task DeleteMessagesAfterDelayAsync(IMessage sentMessage, IMessage? messageToDelete, int delaySeconds)
    {
        try
        {
            await Task.Delay(delaySeconds * 1000);
            await sentMessage.DeleteAsync();
            if (messageToDelete != null)
                await messageToDelete.DeleteAsync();
        }
        catch (Exception ex)
        {
            LogUtil.LogSafe(ex, nameof(QueueModule<T>));
        }
    }

    private static string DeleteTradeCode(ulong userID)
    {
        var tradeCodeStorage = new TradeCodeStorage();
        bool success = tradeCodeStorage.DeleteTradeCode(userID);

        if (success)
            return "Your stored trade code has been deleted successfully.";
        else
            return "No stored trade code found for your user ID.";
    }

    private static string ClearTrade(ulong userID)
    {
        var result = Info.ClearTrade(userID);
        return GetClearTradeMessage(result);
    }

    private static string GetClearTradeMessage(QueueResultRemove result)
    {
        return result switch
        {
            QueueResultRemove.Removed => "Removed your pending trades from the queue.",
            QueueResultRemove.CurrentlyProcessing => "Looks like you have trades currently being processed! Did not remove those from the queue.",
            QueueResultRemove.CurrentlyProcessingRemoved => "Looks like you have trades currently being processed! Removed other pending trades from the queue.",
            QueueResultRemove.NotInQueue => "Sorry, you are not currently in the queue.",
            _ => throw new ArgumentOutOfRangeException(nameof(result), result, null),
        };
    }
}
