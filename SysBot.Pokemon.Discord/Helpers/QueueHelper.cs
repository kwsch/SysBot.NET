using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using Discord.Net;
using PKHeX.Core;

namespace SysBot.Pokemon.Discord;

public static class QueueHelper<T> where T : PKM, new()
{
    private const uint MaxTradeCode = 9999_9999;

    public static async Task AddToQueueAsync(SocketInteractionContext context, int code, RequestSignificance sig, T trade, PokeRoutineType routine, PokeTradeType type, IInteractionContext trader)
    {
        if ((uint)code > MaxTradeCode)
        {
            await context.Interaction.FollowupAsync("Trade code should be 00000000-99999999!").ConfigureAwait(false);
            return;
        }

        try
        {
            var result = AddToTradeQueue(trader, trade, code, sig, routine, type, out var message);
            if (!result)
            {
                await context.Interaction.FollowupAsync(message).ConfigureAwait(false);
                return;
            }

            // Message the user in their DMs. If this fails, the event handler will abort and let them know to enable DMs.
            var channelRef = $"<#{context.Channel.Id}>";
            var secret = $"""
                          {channelRef}
                          {message}
                          I'll message you here when your trade is starting.
                          """;
            var builder = new EmbedBuilder { Color = Color.Blue };
            builder.AddField(x =>
            {
                x.Name = "Trade Code:";
                x.Value = Format.Bold($"{code:0000 0000}");
            });
            var msg = await trader.Interaction.User.SendMessageAsync(secret, embed: builder.Build()).ConfigureAwait(false);

            // Keep a public log of them joining the queue.
            await context.Interaction.Channel.SendMessageAsync($"{trader.User.Mention} - {message}").ConfigureAwait(false);

            // Update the ephermal command message to backlink to the DM we just sent the user.
            await context.Interaction.FollowupAsync($"Please check your direct messages: {msg.GetJumpUrl()}").ConfigureAwait(false);

            // All further communication is in Direct Messages to the user (no further input needed).
        }
        catch (HttpException ex)
        {
            await HandleDiscordExceptionAsync(context, ex).ConfigureAwait(false);
        }
    }

    private static bool AddToTradeQueue(IInteractionContext trader, T pk, int code, RequestSignificance sig, PokeRoutineType routine, PokeTradeType type, out string message)
    {
        var channel = trader.Channel;
        var user = trader.User;
        var userId = user.Id;
        var name = user.Username;
        var trainer = new PokeTradeTrainerInfo(name, userId);
        var notifier = new DiscordTradeNotifier<T>(pk, trainer, code, trader);
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
        {
            message = "Sorry, you are already in the queue.";
            return false;
        }

        var position = info.CheckPosition(userId, routine);
        var ticketId = TradeStartModule<T>.IsStartChannel(channel.Id) ? $", unique ID: {detail.Id}" : "";
        var pokeName = type == PokeTradeType.Specific && pk.Species != 0
            ? $" Receiving: {GameInfo.GetStrings("en").Species[pk.Species]}."
            : "";

        message = $"Added to the {routine} queue{ticketId}. Current Position: {position.Position}.{pokeName}";
        var botct = info.Hub.Bots.Count;
        if (position.Position > botct)
        {
            var eta = info.Hub.Config.Queues.EstimateDelay(position.Position, botct);
            message += $" Estimated: {eta:F1} minutes.";
        }
        return true;
    }

    private static async Task HandleDiscordExceptionAsync(SocketInteractionContext context, HttpException ex)
    {
        string message = string.Empty;
        switch (ex.DiscordCode)
        {
            case DiscordErrorCode.InsufficientPermissions or DiscordErrorCode.MissingPermissions:
                var channel = context.Interaction.Channel;
                IGuild? guild = context.Guild;
                if (guild is not null && channel is IGuildChannel guildChannel)
                {
                    var self = await guild.GetCurrentUserAsync().ConfigureAwait(false);
                    var permissions = self.GetPermissions(guildChannel);
                    if (!permissions.SendMessages)
                    {
                        var app = await context.Client.GetApplicationInfoAsync().ConfigureAwait(false);
                        message = $"{app.Owner.Mention} You must grant me \"Send Messages\" permissions!";
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
