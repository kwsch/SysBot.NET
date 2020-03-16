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
        [Alias("stats")]
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

            var queues = hub.Queues.AllQueues;
            int count = 0;
            foreach (var q in queues)
            {
                var c = q.Count;
                if (c == 0)
                    continue;

                var next = q.TryPeek(out var detail, out _);
                var nextMsg = next ? $"{detail.Trainer.TrainerName} - {detail.TradeData.Nickname}" : "None!";
                builder.AddField(x =>
                {
                    x.Name = $"{q.Type} Queue";
                    x.Value =
                        $"Next: {nextMsg}\n" +
                        $"Count: {c}\n";
                    x.IsInline = false;
                });
                count += c;
            }

            if (count == 0)
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
            var summaries = bots.Select(z => $"- {z.Connection.IP} | {z.Connection.Name} - {z.Config.CurrentRoutineType} ~ {z.LastTime:hh:mm:ss}");
            return Environment.NewLine + string.Join(Environment.NewLine, summaries);
        }

        [Command("startBot")]
        [Summary("Starts a bot by IP address.")]
        [RequireSudo]
        public async Task StartBotAsync(string ip)
        {
            var bot = SysCordInstance.Runner.GetBot(ip);
            if (bot == null)
            {
                await ReplyAsync($"No bot has that IP address ({ip}).").ConfigureAwait(false);
                return;
            }

            bot.Start();
            await ReplyAsync($"The bot at {ip} has been commanded to start.").ConfigureAwait(false);
        }

        [Command("stopBot")]
        [Summary("Stops a bot by IP address.")]
        public async Task StopBotAsync(string ip)
        {
            var bot = SysCordInstance.Runner.GetBot(ip);
            if (bot == null)
            {
                await ReplyAsync($"No bot has that IP address ({ip}).").ConfigureAwait(false);
                return;
            }

            bot.Stop();
            await ReplyAsync($"The bot at {ip} has been commanded to start.").ConfigureAwait(false);
        }
    }
}
