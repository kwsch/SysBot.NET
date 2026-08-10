using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using PKHeX.Core;

namespace SysBot.Pokemon.Discord;

[Group("queue", "Commands to interact with the trade queue.")]
[RequireContext(ContextType.Guild)]
public class QueueModule<T> : SlashModuleBase where T : PKM, new()
{
    private static TradeQueueInfo<T> Info => SysCord<T>.Runner.Hub.Queues.Info;

    [SlashCommand("status", "Checks the user's position in the queue.")]
    public Task GetTradePositionAsync()
        => RespondAsync($"{Context.User.Mention} - {Info.GetPositionString(Context.User.Id)}", ephemeral: true);

    [SlashCommand("clear", "Clears yourself from the trade queues.")]
    public Task ClearTradeAsync()
        => RespondAsync(GetClearTradeMessage(Info.ClearTrade(Context.User.Id)), ephemeral: true);

    /*
     *
     * SUDO COMMANDS BELOW
     *
     */

    [SlashCommand("clear-user", "Clears a user from the trade queues.")]
    [DefaultMemberPermissions(GuildPermission.PrioritySpeaker)] // basic gate to hide the commands from untrusted users, but not a full sudo check
    [RequireSudo]
    public async Task ClearTradeUserAsync(
        [Summary(nameof(user), "The user to clear from the trade queues.")] IUser user)
    {
        var message = GetClearTradeMessage(Info.ClearTrade(user.Id));
        await RespondAsync(message).ConfigureAwait(false);
    }

    [SlashCommand("clear-all", "Clears all users from the trade queues.")]
    [RequireSudo]
    public async Task ClearAllTradesAsync()
    {
        Info.ClearAllQueues();
        await RespondAsync("Cleared all in the queue.").ConfigureAwait(false);
    }

    [SlashCommand("toggle", "Toggles on/off the ability to join the trade queue.")]
    [RequireSudo]
    public async Task ToggleQueueTradeAsync()
    {
        var state = Info.ToggleQueue();
        var message = state
            ? "Users are now able to join the trade queue."
            : $"Changed queue settings: {Format.Bold("Users CANNOT join the queue until it is turned back on.")}";

        await RespondAsync(message).ConfigureAwait(false);
    }

    [SlashCommand("mode", "Changes how queueing is controlled.")]
    [RequireSudo]
    public async Task ChangeQueueModeAsync(
        [Summary(nameof(mode), "The mode to set for queueing.")] QueueOpening mode)
    {
        SysCord<T>.Runner.Hub.Config.Queues.QueueToggleMode = mode;
        await RespondAsync($"Changed queue mode to {mode}.").ConfigureAwait(false);
    }

    [SlashCommand("list", "Sends the list of users in the queue by direct message.")]
    [RequireSudo]
    public async Task ListUserQueue()
    {
        var message = string.Join('\n', Info.GetUserList("(ID {0}) - Code: {1} - {2} - {3}"));
        if (message.Length < 3)
            message = "Queue list is empty.";

        await RespondAsync(message, ephemeral: true).ConfigureAwait(false);
    }

    private static string GetClearTradeMessage(QueueResultRemove result) => result switch
    {
        QueueResultRemove.CurrentlyProcessing => "Looks like you're currently being processed! Did not remove from all queues.",
        QueueResultRemove.CurrentlyProcessingRemoved => "Looks like you're currently being processed!",
        QueueResultRemove.Removed => "Removed you from the queue.",
        _ => "Sorry, you are not currently in the queue.",
    };
}
