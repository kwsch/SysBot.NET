using Discord;
using Discord.Commands;
using PKHeX.Core;
using System.Text;
using System.Threading.Tasks;

namespace SysBot.Pokemon.Discord;

// ReSharper disable once UnusedType.Global
public class BotModule<T> : ModuleBase<SocketCommandContext> where T : PKM, new()
{
    [Command("botStatus")]
    [Summary("Gets the status of the bots.")]
    [RequireSudo]
    public async Task GetStatusAsync()
    {
        var me = SysCord<T>.Runner;
        var sb = new StringBuilder();
        foreach (var bot in me.Bots)
        {
            if (bot.Bot is not PokeRoutineExecutorBase b)
                continue;
            sb.AppendLine(GetDetailedSummary(b));
        }
        if (sb.Length == 0)
        {
            await ReplyAsync("No bots configured.").ConfigureAwait(false);
            return;
        }
        await ReplyAsync(Format.Code(sb.ToString())).ConfigureAwait(false);
    }

    private static string GetDetailedSummary<TBot>(TBot z) where TBot: PokeRoutineExecutorBase
    {
        return $"- {z.Connection.Name} | {z.Connection.Label} - {z.Config.CurrentRoutineType} ~ {z.LastTime:hh:mm:ss} | {z.LastLogged}";
    }

    [Command("botStart")]
    [Summary("Starts a bot by IP address/port.")]
    [RequireSudo]
    public async Task StartBotAsync(string ip)
    {
        var bot = SysCord<T>.Runner.GetBot(ip);
        if (bot == null)
        {
            await ReplyAsync($"No bot has that IP address ({ip}).").ConfigureAwait(false);
            return;
        }

        bot.Start();
        await Context.Channel.EchoAndReply($"The bot at {ip} ({bot.Bot.Connection.Label}) has been commanded to Start.").ConfigureAwait(false);
    }

    [Command("botStop")]
    [Summary("Stops a bot by IP address/port.")]
    [RequireSudo]
    public async Task StopBotAsync(string ip)
    {
        var bot = SysCord<T>.Runner.GetBot(ip);
        if (bot == null)
        {
            await ReplyAsync($"No bot has that IP address ({ip}).").ConfigureAwait(false);
            return;
        }

        bot.Stop();
        await Context.Channel.EchoAndReply($"The bot at {ip} ({bot.Bot.Connection.Label}) has been commanded to Stop.").ConfigureAwait(false);
    }

    [Command("botIdle")]
    [Alias("botPause")]
    [Summary("Commands a bot to Idle by IP address/port.")]
    [RequireSudo]
    public async Task IdleBotAsync(string ip)
    {
        var bot = SysCord<T>.Runner.GetBot(ip);
        if (bot == null)
        {
            await ReplyAsync($"No bot has that IP address ({ip}).").ConfigureAwait(false);
            return;
        }

        bot.Pause();
        await Context.Channel.EchoAndReply($"The bot at {ip} ({bot.Bot.Connection.Label}) has been commanded to Idle.").ConfigureAwait(false);
    }

    [Command("botChange")]
    [Summary("Changes the routine of a bot (trades).")]
    [RequireSudo]
    public async Task ChangeTaskAsync(string ip, [Summary("Routine enum name")] PokeRoutineType task)
    {
        var bot = SysCord<T>.Runner.GetBot(ip);
        if (bot == null)
        {
            await ReplyAsync($"No bot has that IP address ({ip}).").ConfigureAwait(false);
            return;
        }

        bot.Bot.Config.Initialize(task);
        await Context.Channel.EchoAndReply($"The bot at {ip} ({bot.Bot.Connection.Label}) has been commanded to do {task} as its next task.").ConfigureAwait(false);
    }

    [Command("botRestart")]
    [Summary("Restarts the bot(s) by IP address(es), separated by commas.")]
    [RequireSudo]
    public async Task RestartBotAsync(string ipAddressesCommaSeparated)
    {
        var ips = ipAddressesCommaSeparated.Split(',');
        foreach (var ip in ips)
        {
            var bot = SysCord<T>.Runner.GetBot(ip);
            if (bot == null)
            {
                await ReplyAsync($"No bot has that IP address ({ip}).").ConfigureAwait(false);
                return;
            }

            var c = bot.Bot.Connection;
            c.Reset();
            bot.Start();
            await Context.Channel.EchoAndReply($"The bot at {ip} ({c.Label}) has been commanded to Restart.").ConfigureAwait(false);
        }
    }
}
