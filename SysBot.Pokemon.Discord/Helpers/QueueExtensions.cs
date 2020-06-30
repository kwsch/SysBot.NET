using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.Net;
using PKHeX.Core;

namespace SysBot.Pokemon.Discord
{
    public static class QueueExtensions
    {
        private const uint MaxTradeCode = 99999999;

        public static async Task AddToQueueAsync(this SocketCommandContext Context, int code, string trainer, bool sudo, PK8 trade, PokeRoutineType routine, PokeTradeType type)
        {
            if ((uint)code > MaxTradeCode)
            {
                await Context.Channel.SendMessageAsync("Trade code should be 00000000-99999999!").ConfigureAwait(false);
                return;
            }

            IUserMessage test;
            try
            {
                const string helper = "I've added you to the queue! I'll message you here when your trade is starting.";
                test = await Context.User.SendMessageAsync(helper).ConfigureAwait(false);
            }
            catch (HttpException ex)
            {
                await Context.Channel.SendMessageAsync($"{ex.HttpCode}: {ex.Reason}!").ConfigureAwait(false);
                await Context.Channel.SendMessageAsync("You must enable private messages in order to be queued!").ConfigureAwait(false);
                return;
            }

            // Try adding
            var result = Context.AddToTradeQueue(trade, code, trainer, sudo, routine, type, out var msg);

            // Notify in channel
            await Context.Channel.SendMessageAsync(msg).ConfigureAwait(false);
            // Notify in PM to mirror what is said in the channel.
            await Context.User.SendMessageAsync(msg).ConfigureAwait(false);

            // Clean Up
            if (result)
            {
                // Delete the user's join message for privacy
                if (!Context.IsPrivate)
                    await Context.Message.DeleteAsync(RequestOptions.Default).ConfigureAwait(false);
            }
            else
            {
                // Delete our "I'm adding you!", and send the same message that we sent to the general channel.
                await test.DeleteAsync().ConfigureAwait(false);
            }
        }

        private static bool AddToTradeQueue(this SocketCommandContext Context, PK8 pk8, int code, string trainerName, bool sudo, PokeRoutineType type, PokeTradeType t, out string msg)
        {
            var user = Context.User;
            var userID = user.Id;
            var name = user.Username;

            var trainer = new PokeTradeTrainerInfo(trainerName);
            var notifier = new DiscordTradeNotifier<PK8>(pk8, trainer, code, Context);
            var detail = new PokeTradeDetail<PK8>(pk8, trainer, notifier, t, code: code);
            var trade = new TradeEntry<PK8>(detail, userID, type, name);

            var hub = SysCordInstance.Self.Hub;
            var Info = hub.Queues.Info;
            var added = Info.AddToTradeQueue(trade, userID, sudo);

            if (added == QueueResultAdd.AlreadyInQueue)
            {
                msg = "Sorry, you are already in the queue.";
                return false;
            }

            var position = Info.CheckPosition(userID, type);

            var ticketID = "";
            if (TradeStartModule.IsStartChannel(Context.Channel.Id))
                ticketID = $", unique ID: {detail.ID}";
            
            var displayRequest = Info.Hub.Config.Discord.DisplayPokeName;
            if (displayRequest) {
                var pokeName = $"{(Species)pk8.Species}";
                if (!pokeName.Trim().Equals("None"))
                    msg = $"{user.Mention} - Added to the {type} queue{ticketID}. Current Position: {position.Position}. You asked for {pokeName}.";
                else
                    msg = $"{user.Mention} - Added to the {type} queue{ticketID}. Current Position: {position.Position}";
            }
            
            else msg = $"{user.Mention} - Added to the {type} queue{ticketID}. Current Position: {position.Position}";

            var botct = Info.Hub.Bots.Count;
            if (position.Position > botct)
            {
                var eta = Info.Hub.Config.Queues.EstimateDelay(position.Position, botct);
                msg += $". Estimated: {eta:F1} minutes.";
            }
            return true;
        }
    }
}
