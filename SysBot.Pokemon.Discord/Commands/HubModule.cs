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
                    $"Pool Count: {hub.Ledy.Pool.Count}\n";
                x.IsInline = false;
            });

            builder.AddField(x =>
            {
                var msg = string.Join("\n", hub.Counts.Summary());
                if (string.IsNullOrWhiteSpace(msg))
                    msg = "Nothing counted yet!";
                x.Name = "Counts";
                x.Value = msg;
                x.IsInline = false;
            });

            var count = hub.Queues.Trade.Count;
            if (count != 0)
            {
                var next = hub.Queues.Trade.TryPeek(out var detail, out _);
                var nextMsg = next ? $"{detail.Trainer.TrainerName} - {detail.TradeData.Nickname}" : "None!";
                builder.AddField(x =>
                {
                    x.Name = "Trade Queue";
                    x.Value =
                        $"Next: {nextMsg}\n" +
                        $"Count: {count}\n";
                    x.IsInline = false;
                });
            }

            var countC = hub.Queues.Clone.Count;
            if (countC != 0)
            {
                var nextC = hub.Queues.Clone.TryPeek(out var detailC, out _);
                var nextMsgC = nextC ? $"{detailC.Trainer.TrainerName}" : "None!";
                builder.AddField(x =>
                {
                    x.Name = "Clone Queue";
                    x.Value =
                        $"Next: {nextMsgC}\n" +
                        $"Count: {countC}\n";
                    x.IsInline = false;
                });
            }

            var countD = hub.Queues.Dudu.Count;
            if (countD != 0)
            {
                var nextD = hub.Queues.Dudu.TryPeek(out var detailD, out _);
                var nextMsgD = nextD ? $"{detailD.Trainer.TrainerName}" : "None!";
                builder.AddField(x =>
                {
                    x.Name = "Dudu Queue";
                    x.Value =
                        $"Next: {nextMsgD}\n" +
                        $"Count: {countD}\n";
                    x.IsInline = false;
                });
            }

            if (count + countC + countD == 0)
            {
                builder.AddField(x =>
                {
                    x.Name = "Queues are empty.";
                    x.Value = "Nobody in line!";
                    x.IsInline = false;
                });
            }

            await ReplyAsync("Bot Status", false, builder.Build()).ConfigureAwait(false);
        }

        private static string SummarizeBots(PokeTradeHub<PK8> hub)
        {
            var bots = hub.Bots.ToArray();
            if (bots.Length == 0)
                return "No bots configured.";
            var summaries = bots.Select(z => $"- {z.Connection.Name} - {z.Config.CurrentRoutineType}");
            return Environment.NewLine + string.Join(Environment.NewLine, summaries);
        }
    }
}
