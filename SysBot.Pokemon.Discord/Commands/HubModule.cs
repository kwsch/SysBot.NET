using System;
using System.Linq;
using System.Threading.Tasks;
using Discord.Commands;
using PKHeX.Core;

namespace SysBot.Pokemon.Discord
{
    public class HubModule : ModuleBase<SocketCommandContext>
    {
        [Command("status")]
        [Summary("Gets the status of the bot environment.")]
        public async Task GetStatusAsync()
        {
            var me = SysCordInstance.Self;
            var hub = me.Hub;

            var next = hub.Queue.TryPeek(out var detail, out _);
            var nextMsg = next ? $"{detail.Trainer.TrainerName} - {detail.TradeData.Nickname}" : "None!";

            var botMsg = SummarizeBots(hub);

            var msg =
                $"Bot Count: {hub.Bots.Count}\n" +
                $"Bot State: {botMsg}\n" +
                $"Completed Trades: {hub.Config.CompletedTrades}\n" +
                $"Pool Count: {hub.Pool.Count}\n" +
                $"Trade Queue Count: {hub.Queue.Count}\n" +
                $"Trade Queue Next: {nextMsg}";
            await ReplyAsync(msg).ConfigureAwait(false);
        }

        private static string SummarizeBots(PokeTradeHub<PK8> hub)
        {
            var bots = hub.Bots.ToArray();
            if (bots.Length == 0)
                return "No bots configured.";
            var summaries = bots.Select(z => $"{z.Connection.Name} - {z.Config.CurrentRoutineType}");
            return string.Join(Environment.NewLine, summaries);
        }
    }
}