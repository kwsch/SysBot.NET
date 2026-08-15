using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using PKHeX.Core;
using SysBot.Base;

namespace SysBot.Pokemon.Discord;

[RequireContext(ContextType.Guild)]
public class HubModule<T> : SlashModuleBase where T : PKM, new()
{
    [SlashCommand("status", "Gets the status of the bot environment.")]
    public async Task GetStatusAsync()
    {
        var me = SysCord<T>.Runner;
        var hub = me.Hub;

        var builder = new EmbedBuilder { Color = Color.Gold };
        var all = me.Bots.ConvertAll(z => z.Bot);
        builder.AddField(x =>
        {
            x.Name = "Summary";
            x.Value =
                $"Bot Count: {all.Count}\n" +
                $"Bot State: {SummarizeBots(all)}\n" +
                $"Pool Count: {hub.Ledy.Pool.Count}\n";
            x.IsInline = false;
        });

        builder.AddField(x =>
        {
            var lines = all.OfType<ICountBot>().SelectMany(z => z.Counts.GetNonZeroCounts()).Distinct();
            x.Name = "Counts";
            x.Value = string.Join('\n', lines) is { Length: > 0 } msg ? msg : "Nothing counted yet!";
            x.IsInline = false;
        });

        int count = 0;
        foreach (var q in hub.Queues.AllQueues)
        {
            if (q.Count == 0)
                continue;
            var next = GetNextName(q);
            builder.AddField(x =>
            {
                x.Name = $"{q.Type} Queue";
                x.Value = $"Next: {next}\nCount: {q.Count}\n";
                x.IsInline = false;
            });
            count += q.Count;
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

        await RespondAsync("Bot Status", ephemeral: true, embed: builder.Build()).ConfigureAwait(false);
    }

    private static string GetNextName(PokeTradeQueue<T> q)
    {
        if (!q.TryPeek(out var detail, out _, checkReady: false)) // can be soon-ready
            return "None!";

        var name = detail.Trainer.TrainerName;

        // show detail of trade if possible
        var nick = detail.TradeData.Nickname;
        return string.IsNullOrEmpty(nick) ? name : $"{name} - {nick}";
    }

    private static string SummarizeBots(List<RoutineExecutor<PokeBotState>> bots)
    {
        if (bots.Count == 0)
            return "No bots configured.";
        var summaries = bots.Select(z => $"- {z.GetSummary()}");
        return Environment.NewLine + string.Join(Environment.NewLine, summaries);
    }
}
