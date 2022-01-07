﻿using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Discord.Net;
using PKHeX.Core;
using System.Threading.Tasks;

namespace SysBot.Pokemon.Discord
{
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

            IUserMessage test;
            try
            {
                const string helper = "I've added you to the queue! I'll message you here when your trade is starting.";
                test = await trader.SendMessageAsync(helper).ConfigureAwait(false);
            }
            catch (HttpException ex)
            {
                await context.Channel.SendMessageAsync($"{ex.HttpCode}: {ex.Reason}!").ConfigureAwait(false);
                var noAccessMsg = context.User == trader ? "You must enable private messages in order to be queued!" : "The mentioned user must enable private messages in order for them to be queued!";
                await context.Channel.SendMessageAsync(noAccessMsg).ConfigureAwait(false);
                return;
            }

            // Try adding
            var result = AddToTradeQueue(context, trade, code, trainer, sig, routine, type, trader, out var msg);

            // Notify in channel
            await context.Channel.SendMessageAsync(msg).ConfigureAwait(false);
            // Notify in PM to mirror what is said in the channel.
            await trader.SendMessageAsync($"{msg}\nYour trade code will be **{code:0000 0000}**.").ConfigureAwait(false);

            try
            {
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
                string message;
                // Check if the exception was raised due to missing "Manage Messages" permissions. Ping the bot host if so.
                var permissions = context.Guild.CurrentUser.GetPermissions(context.Channel as IGuildChannel);
                if (!permissions.ManageMessages)
                {
                    var app = await context.Client.GetApplicationInfoAsync().ConfigureAwait(false);
                    var owner = app.Owner.Id;
                    message = $"<@{owner}> You must grant me \"Manage Messages\" permissions!";
                }
                else
                {
                    // Send a generic error message if we're not missing "Manage Messages" permissions.
                    message = $"{ex.HttpCode}: {ex.Reason}!";
                }
                await context.Channel.SendMessageAsync(message).ConfigureAwait(false);
            }
        }

        public static async Task AddToQueueAsync(SocketCommandContext context, int code, string trainer, RequestSignificance sig, T trade, PokeRoutineType routine, PokeTradeType type)
        {
            await AddToQueueAsync(context, code, trainer, sig, trade, routine, type, context.User).ConfigureAwait(false);
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
                pokeName = $" Receiving: {(Species)pk.Species}.";
            msg = $"{user.Mention} - Added to the {type} queue{ticketID}. Current Position: {position.Position}.{pokeName}";

            var botct = Info.Hub.Bots.Count;
            if (position.Position > botct)
            {
                var eta = Info.Hub.Config.Queues.EstimateDelay(position.Position, botct);
                msg += $" Estimated: {eta:F1} minutes.";
            }
            return true;
        }
    }
}