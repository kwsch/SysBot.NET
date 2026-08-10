using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using PKHeX.Core;

namespace SysBot.Pokemon.Discord;

[Group("bot", "Bot manual control commands.")]
public class BotModule<T> : SudoModuleBase where T : PKM, new()
{
    [SlashCommand("status", "Gets the status of the bots.")]
    public async Task GetStatusAsync()
    {
        var sb = new StringBuilder();
        foreach (var bot in SysCord<T>.Runner.Bots)
        {
            if (bot.Bot is PokeRoutineExecutorBase b)
                sb.AppendLine(GetDetailedSummary(b));
        }

        await RespondAsync(sb.Length == 0 ? "No bots configured." : Format.Code(sb.ToString())).ConfigureAwait(false);
    }

    [SlashCommand("start", "Starts a bot by IP address/port.")]
    public async Task StartBotAsync(string ip)
    {
        var bot = SysCord<T>.Runner.GetBot(ip);
        if (bot == null)
        {
            await RespondAsync($"No bot has that IP address ({ip}).").ConfigureAwait(false);
            return;
        }

        bot.Start();
        await RespondAsync($"The bot at {ip} ({bot.Bot.Connection.Label}) has been commanded to Start.").ConfigureAwait(false);
    }

    [SlashCommand("stop", "Stops a bot by IP address/port.")]
    public async Task StopBotAsync(string ip)
    {
        var bot = SysCord<T>.Runner.GetBot(ip);
        if (bot == null)
        {
            await RespondAsync($"No bot has that IP address ({ip}).").ConfigureAwait(false);
            return;
        }

        bot.Stop();
        await RespondAsync($"The bot at {ip} ({bot.Bot.Connection.Label}) has been commanded to Stop.").ConfigureAwait(false);
    }

    [SlashCommand("idle", "Commands a bot to Idle by IP address/port.")]
    public async Task IdleBotAsync(string ip)
    {
        var bot = SysCord<T>.Runner.GetBot(ip);
        if (bot == null)
        {
            await RespondAsync($"No bot has that IP address ({ip}).").ConfigureAwait(false);
            return;
        }

        bot.Pause();
        await RespondAsync($"The bot at {ip} ({bot.Bot.Connection.Label}) has been commanded to Idle.").ConfigureAwait(false);
    }

    [SlashCommand("change", "Changes the routine of a bot.")]
    public async Task ChangeTaskAsync(string ip, PokeRoutineType task)
    {
        var bot = SysCord<T>.Runner.GetBot(ip);
        if (bot == null)
        {
            await RespondAsync($"No bot has that IP address ({ip}).").ConfigureAwait(false);
            return;
        }

        bot.Bot.Config.Initialize(task);
        await RespondAsync($"The bot at {ip} ({bot.Bot.Connection.Label}) has been commanded to do {task} as its next task.").ConfigureAwait(false);
    }

    [SlashCommand("restart", "Restarts bots by comma-separated IP addresses.")]
    public async Task RestartBotAsync(string ipAddressesCommaSeparated)
    {
        var messages = new List<string>();
        foreach (var ip in ipAddressesCommaSeparated.Split(','))
        {
            var bot = SysCord<T>.Runner.GetBot(ip);
            if (bot == null)
            {
                messages.Add($"No bot has that IP address ({ip}).");
                continue;
            }

            var c = bot.Bot.Connection;
            c.Reset();
            bot.Start();
            messages.Add($"The bot at {ip} ({c.Label}) has been commanded to Restart.");
        }

        await RespondAsync(string.Join('\n', messages)).ConfigureAwait(false);
    }
    private static string GetDetailedSummary<TBot>(TBot z) where TBot : PokeRoutineExecutorBase =>
        $"- {z.Connection.Name} | {z.Connection.Label} - {z.Config.CurrentRoutineType} ~ {z.LastTime:hh:mm:ss} | {z.LastLogged}";
}
