using Discord;
using Discord.Commands;
using Discord.Net;
using Discord.WebSocket;
using PKHeX.Core;
using System.Threading.Tasks;

namespace SysBot.Pokemon.Discord;

public static class QueueHelper<T> where T : PKM, new()
{
    private const uint MaxTradeCode = 9999_9999;

    public static async Task AddToQueueAsync(SocketCommandContext context, int code, string trainer, RequestSignificance sig, T trade, PokeRoutineType routine, PokeTradeType type, SocketUser trader)
    {
        if ((uint)code > MaxTradeCode)
        {
            await context.Channel.SendMessageAsync("Trade code should be 00000000-99999999!").ConfigureAwait(false);
            return;
        }

        try
        {
            const string helper = "I've added you to the queue! I'll message you here when your trade is starting.";
            IUserMessage test = await trader.SendMessageAsync(helper).ConfigureAwait(false);

            // Try adding
            var result = AddToTradeQueue(context, trade, code, trainer, sig, routine, type, trader, out var msg);

            // Notify in channel
            await context.Channel.SendMessageAsync(msg).ConfigureAwait(false);
            // Notify in PM to mirror what was said in the channel.
            // Only tell them a trade code if it was successful.
            if (result)
                msg += $"\nYour trade code will be **{code:0000 0000}**.";
            await trader.SendMessageAsync($"{msg}").ConfigureAwait(false);

            // Clean Up
            if (result)
            {
                // Delete the user's join message for privacy
                if (!context.IsPrivate)
                    await context.Message.DeleteAsync(RequestOptions.Default).ConfigureAwait(false);
            }
            else
            {
                // Delete our "I'm adding you!", and send the same message that we sent to the general channel.
                await test.DeleteAsync().ConfigureAwait(false);
            }
        }
        catch (HttpException ex)
        {
            await HandleDiscordExceptionAsync(context, trader, ex).ConfigureAwait(false);
        }
    }

    public static Task AddToQueueAsync(SocketCommandContext context, int code, string trainer, RequestSignificance sig, T trade, PokeRoutineType routine, PokeTradeType type)
    {
        return AddToQueueAsync(context, code, trainer, sig, trade, routine, type, context.User);
    }

    private static bool AddToTradeQueue(SocketCommandContext context, T pk, int code, string trainerName, RequestSignificance sig, PokeRoutineType type, PokeTradeType t, SocketUser trader, out string msg)
    {
        var user = trader;
        var userID = user.Id;
        var name = user.Username;

        var trainer = new PokeTradeTrainerInfo(trainerName, userID);
        var notifier = new DiscordTradeNotifier<T>(pk, trainer, code, user);
        var detail = new PokeTradeDetail<T>(pk, trainer, notifier, t, code, sig == RequestSignificance.Favored);
        var trade = new TradeEntry<T>(detail, userID, type, name);

        var hub = SysCord<T>.Runner.Hub;
        var Info = hub.Queues.Info;
        var added = Info.AddToTradeQueue(trade, userID, sig == RequestSignificance.Owner);

        if (added == QueueResultAdd.AlreadyInQueue)
        {
            msg = "Sorry, you are already in the queue.";
            return false;
        }

        var position = Info.CheckPosition(userID, type);

        var ticketID = "";
        if (TradeStartModule<T>.IsStartChannel(context.Channel.Id))
            ticketID = $", unique ID: {detail.ID}";

        var pokeName = "";
        if (t == PokeTradeType.Specific && pk.Species != 0)
            pokeName = $" Receiving: {GameInfo.GetStrings("en").Species[pk.Species]}.";
        msg = $"{user.Mention} - Added to the {type} queue{ticketID}. Current Position: {position.Position}.{pokeName}";

        var botct = Info.Hub.Bots.Count;
        if (position.Position > botct)
        {
            var eta = Info.Hub.Config.Queues.EstimateDelay(position.Position, botct);
            msg += $" Estimated: {eta:F1} minutes.";
        }
        return true;
    }

    private static async Task HandleDiscordExceptionAsync(SocketCommandContext context, SocketUser trader, HttpException ex)
    {
        string message = string.Empty;
        switch (ex.DiscordCode)
        {
            case DiscordErrorCode.InsufficientPermissions or DiscordErrorCode.MissingPermissions:
            {
                // Check if the exception was raised due to missing "Send Messages" or "Manage Messages" permissions. Nag the bot owner if so.
                var permissions = context.Guild.CurrentUser.GetPermissions(context.Channel as IGuildChannel);
                if (!permissions.SendMessages)
                {
                    // Nag the owner in logs.
                    message = "You must grant me \"Send Messages\" permissions!";
                    Base.LogUtil.LogError(message, "QueueHelper");
                    return;
                }
                if (!permissions.ManageMessages)
                {
                    var app = await context.Client.GetApplicationInfoAsync().ConfigureAwait(false);
                    var owner = app.Owner.Id;
                    message = $"<@{owner}> You must grant me \"Manage Messages\" permissions!";
                }
            }
                break;
            case DiscordErrorCode.CannotSendMessageToUser:
            {
                // The user either has DMs turned off, or Discord thinks they do.
                message = context.User == trader ? "You must enable private messages in order to be queued!" : "The mentioned user must enable private messages in order for them to be queued!";
            }
                break;
            default:
            {
                // Send a generic error message.
                message = ex.DiscordCode != null ? $"Discord error {(int)ex.DiscordCode}: {ex.Reason}" : $"Http error {(int)ex.HttpCode}: {ex.Message}";
            }
                break;
        }
        await context.Channel.SendMessageAsync(message).ConfigureAwait(false);
    }
}
