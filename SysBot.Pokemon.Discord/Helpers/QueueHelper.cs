using System.IO;
using System.Threading.Tasks;
using Discord;
using Discord.Net;
using PKHeX.Core;

namespace SysBot.Pokemon.Discord;

public static class QueueHelper<T> where T : PKM, new()
{
    public static async Task AddToQueueAsync(IInteractionContext context, int code, T pk, PokeRoutineType routine, PokeTradeType type)
    {
        QueueJoinResult? check = null;
        try
        {
            check = AddToTradeQueue(context, pk, code, routine, type);
            var result = check.Result;
            if (!result)
            {
                await context.Interaction.FollowupAsync(check.Message).ConfigureAwait(false);
                return;
            }

            // Message the user in their DMs. If this fails, the event handler will abort and let them know to enable DMs.
            var task = GetPrivateMessageTradeJoin(context, pk, check, out var sprite);
            var message = await task.ConfigureAwait(false);
            if (sprite != null)
                await sprite.DisposeAsync().ConfigureAwait(false);

            // Keep a public log of them joining the queue.
            await context.Channel.SendMessageAsync($"{context.User.Mention} - {check.Message}").ConfigureAwait(false);

            // Update the ephemeral command message to backlink to the DM we just sent the user.
            await context.Interaction.FollowupAsync($"Success! Please check your direct messages: {message.GetJumpUrl()}").ConfigureAwait(false);

            // All further communication is in Direct Messages to the user (no further input needed).
            check.Join.Trade.IsReady = true; // If we failed, we'd instead remove via the exception handling below.
        }
        catch (HttpException ex)
        {
            // They might have been added to the queue with DMs off; dequeue them immediately if so.
            if (check?.Result is true)
            {
                var detail = check.Join;
                var hub = SysCord<T>.Runner.Hub;
                var info = hub.Queues.Info;
                info.Remove(detail);
            }

            await HandleDiscordExceptionAsync(context, ex).ConfigureAwait(false);
        }
    }

    private static Task<IUserMessage> GetPrivateMessageTradeJoin(IInteractionContext context, T pk, QueueJoinResult check,
        out MemoryStream? sprite)
    {
        // Prepend the embed with a message letting the user know about the trade.
        var channelRef = $"<#{context.Channel.Id}>";
        var secret = $"""
                      {channelRef}
                      {check.Message}
                      I'll message you here when your trade is starting.
                      """;
        return GetPrivateMessageTradeJoin(context, pk, check, secret, out sprite);
    }

    private static Task<IUserMessage> GetPrivateMessageTradeJoin(IInteractionContext context, T pk, QueueJoinResult check, string message,
        out MemoryStream? sprite)
    {
        var builder = new EntityEmbedBuilder(pk);
        builder
            .AddReceiving()
            .AddTradeCode(check.Join.Trade.Code)
            .AddQueuePosition(check.Position);

        var user = context.Interaction.User;
        if (builder.TryAddSpriteThumbnail(out sprite, out var thumb))
            return user.SendFileAsync(thumb.Value, text: message, embed: builder.Build());

        // No sprite, just return a regular message.
        return user.SendMessageAsync(text: message, embed: builder.Build());
    }

    /// <summary>
    /// Represents the result of attempting to join a trade queue, including whether the join was successful, the trade entry, and an associated message.
    /// </summary>
    /// <param name="Result">Indicates whether the join was successful.</param>
    /// <param name="Join">The trade entry associated with the join attempt.</param>
    /// <param name="Message">A message providing additional information about the join attempt.</param>
    /// <param name="Position">The position within the queue that the user joined at.</param>
    /// <param name="Estimate">Estimated time (in minutes) that the user will need to wait before a bot picks up their request.</param>
    private sealed record QueueJoinResult(bool Result, TradeEntry<T> Join, string Message, int Position = 0, float Estimate = 0);

    private static QueueJoinResult AddToTradeQueue(IInteractionContext trader, T pk, int code, PokeRoutineType routine, PokeTradeType type)
    {
        var channel = trader.Channel;
        var user = trader.User;
        var userId = user.Id;
        var name = user.Username;
        var trainer = new PokeTradeTrainerInfo(name, userId);
        var notifier = new DiscordTradeNotifier<T>(pk, trainer, code, trader);
        var sig = trader.GetSignificance();
        var detail = new PokeTradeDetail<T>
        {
            Type = type,
            Code = code,
            TradeData = pk,
            Trainer = trainer,
            Notifier = notifier,
            IsFavored = sig == RequestSignificance.Favored,
        };
        var trade = new TradeEntry<T>(detail, userId, routine, name);

        var hub = SysCord<T>.Runner.Hub;
        var info = hub.Queues.Info;
        var added = info.AddToTradeQueue(trade, userId, sig == RequestSignificance.Owner);
        if (added == QueueResultAdd.AlreadyInQueue)
            return new(false, trade, "Sorry, you are already in the queue.");

        var position = info.CheckPosition(userId, routine);
        var ticketId = TradeStartModule<T>.IsStartChannel(channel.Id) ? $", unique ID: {detail.Id}" : "";
        var pokeName = type == PokeTradeType.Specific && pk.Species != 0
            ? $" Receiving: {GameInfo.GetStrings("en").Species[pk.Species]}."
            : "";

        var message = $"Added to the {routine} queue{ticketId}. Current Position: {position.Position}.{pokeName}";
        var botct = info.Hub.Bots.Count;
        float estimate = 0;
        if (position.Position > botct)
        {
            estimate = info.Hub.Config.Queues.EstimateDelay(position.Position, botct);
            message += $" Estimated: {estimate:F1} minutes.";
        }
        // Don't mark as ready yet; notifying the user may fail (DMs disabled). If so, we'll remove from the queue and not mark as ready.
        return new(true, trade, message, position.Position, estimate);
    }

    private static async Task HandleDiscordExceptionAsync(IInteractionContext context, HttpException ex)
    {
        string message = string.Empty;
        switch (ex.DiscordCode)
        {
            case DiscordErrorCode.InsufficientPermissions or DiscordErrorCode.MissingPermissions:
                var channel = context.Channel;
                IGuild? guild = context.Guild;
                if (guild is not null && channel is IGuildChannel guildChannel)
                {
                    var self = await guild.GetCurrentUserAsync().ConfigureAwait(false);
                    var permissions = self.GetPermissions(guildChannel);
                    if (!permissions.SendMessages)
                    {
                        message = $"{SysCordSettings.Manager.Owner.Mention} - You must grant me \"Send Messages\" permissions!";
                        Base.LogUtil.LogError(message);
                        return;
                    }
                }
                break;
            case DiscordErrorCode.CannotSendMessagesToThisUserDueToHavingNoMutualGuilds:
            case DiscordErrorCode.CannotSendMessageToUser:
                message = "You must enable private messages in order to be queued!";
                break;
            default:
                message = ex.DiscordCode != null
                    ? $"Discord error {(int)ex.DiscordCode}: {ex.Reason}"
                    : $"Http error {(int)ex.HttpCode}: {ex.Message}";
                break;
        }

        if (string.IsNullOrWhiteSpace(message))
            return;

        var interaction = context.Interaction;
        // Can still respond to their command.
        await interaction.FollowupAsync(message, ephemeral: true).ConfigureAwait(false);
    }
}
