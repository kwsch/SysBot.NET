using System;
using System.Linq;
using System.Threading.Tasks;
using Discord;
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

            var builder = new EmbedBuilder
            {
                Color = Color.Gold,
            };
            builder.AddField(x =>
            {
                x.Name = "Summary";
                x.Value =
                    $"Bot Count: {hub.Bots.Count}\n" +
                    $"Bot State: {SummarizeBots(hub)}\n" +
                    $"Pool Count: {hub.Pool.Count}\n";
                x.IsInline = false;
            });

            builder.AddField(x =>
            {
                x.Name = "Counts";
                x.Value =
                    $"Completed Trades: {hub.Config.CompletedTrades}\n" +
                    $"Distribution Trades: {hub.Config.CompletedDistribution}\n" +
                    $"Surprise Trades: {hub.Config.CompletedSurprise}\n" +
                    $"Eggs Received: {hub.Config.CompletedEggs}\n" +
                    $"Dudu Trades: {hub.Config.CompletedDudu}\n";
                x.IsInline = false;
            });

            var next = hub.Queue.TryPeek(out var detail, out _);
            var nextMsg = next ? $"{detail.Trainer.TrainerName} - {detail.TradeData.Nickname}" : "None!";
            var count = hub.Queue.Count;
            builder.AddField(x =>
            {
                x.Name = "Trade Queue";
                x.Value =
                    $"Next: {nextMsg}\n" +
                    $"Count: {count}\n";
                x.IsInline = false;
            });

            var nextD = hub.Dudu.TryPeek(out var detailD, out _);
            var nextMsgD = nextD ? $"{detailD.Trainer.TrainerName}" : "None!";
            var countD = hub.Dudu.Count;
            builder.AddField(x =>
            {
                x.Name = "Dudu Queue";
                x.Value =
                    $"Next: {nextMsgD}\n" +
                    $"Count: {countD}\n";
                x.IsInline = false;
            });
            await ReplyAsync("Bot Status", false, builder.Build()).ConfigureAwait(false);
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