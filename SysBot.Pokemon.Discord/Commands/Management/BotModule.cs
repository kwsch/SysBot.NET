using Discord;
using Discord.Commands;
using PKHeX.Core;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace SysBot.Pokemon.Discord;

public class BotModule<T> : ModuleBase<SocketCommandContext> where T : PKM, new()
{
    [Command("botStatus")]
    [Summary("Ermittelt den Status der Bots.")]
    [RequireSudo]
    public async Task GetStatusAsync()
    {
        var me = SysCord<T>.Runner;
        var bots = me.Bots.Select(z => z.Bot).OfType<PokeRoutineExecutorBase>().ToArray();
        if (bots.Length == 0)
        {
            await ReplyAsync("Keine Bots konfiguriert.").ConfigureAwait(false);
            return;
        }

        var summaries = bots.Select(GetDetailedSummary);
        var lines = string.Join(Environment.NewLine, summaries);
        await ReplyAsync(Format.Code(lines)).ConfigureAwait(false);
    }

    private static string GetDetailedSummary(PokeRoutineExecutorBase z)
    {
        return $"- {z.Connection.Name} | {z.Connection.Label} - {z.Config.CurrentRoutineType} ~ {z.LastTime:hh:mm:ss} | {z.LastLogged}";
    }

    [Command("botStart")]
    [Summary("Startet einen Bot nach IP-Adresse/Port.")]
    [RequireSudo]
    public async Task StartBotAsync(string ip)
    {
        var bot = SysCord<T>.Runner.GetBot(ip);
        if (bot == null)
        {
            await ReplyAsync($"Kein Bot hat diese IP-Adresse ({ip}).").ConfigureAwait(false);
            return;
        }

        bot.Start();
        await Context.Channel.EchoAndReply($"Der Bot auf {ip} ({bot.Bot.Connection.Label}) hat den Befehl zum Starten erhalten.").ConfigureAwait(false);
    }

    [Command("botStop")]
    [Summary("Stoppt einen Bot nach IP-Adresse/Port.")]
    [RequireSudo]
    public async Task StopBotAsync(string ip)
    {
        var bot = SysCord<T>.Runner.GetBot(ip);
        if (bot == null)
        {
            await ReplyAsync($"Kein Bot hat die IP-Adresse ({ip}).").ConfigureAwait(false);
            return;
        }

        bot.Stop();
        await Context.Channel.EchoAndReply($"Der Bot auf {ip} ({bot.Bot.Connection.Label}) hat den Befehl zum Anhalten erhalten.").ConfigureAwait(false);
    }

    [Command("botIdle")]
    [Alias("botPause")]
    [Summary("Veranlasst einen Bot per IP-Adresse/Port zum Idle-Modus.")]
    [RequireSudo]
    public async Task IdleBotAsync(string ip)
    {
        var bot = SysCord<T>.Runner.GetBot(ip);
        if (bot == null)
        {
            await ReplyAsync($"Kein Bot hat die IP-Adresse ({ip}).").ConfigureAwait(false);
            return;
        }

        bot.Pause();
        await Context.Channel.EchoAndReply($"Der Bot auf {ip} ({bot.Bot.Connection.Label}) wurde in den Leerlauf befohlen.").ConfigureAwait(false);
    }

    [Command("botChange")]
    [Summary("Ändert die Routine eines Bots (Trades).")]
    [RequireSudo]
    public async Task ChangeTaskAsync(string ip, [Summary("Routine enum name")] PokeRoutineType task)
    {
        var bot = SysCord<T>.Runner.GetBot(ip);
        if (bot == null)
        {
            await ReplyAsync($"Kein Bot hat die IP-Adresse ({ip}).").ConfigureAwait(false);
            return;
        }

        bot.Bot.Config.Initialize(task);
        await Context.Channel.EchoAndReply($"Der Bot auf {ip} ({bot.Bot.Connection.Label}) hat den Befehl erhalten, {task} als nächste Aufgabe zu erledigen.").ConfigureAwait(false);
    }

    [Command("botRestart")]
    [Summary("Startet den/die Bot(s) nach IP-Adresse(n), getrennt durch Kommas, neu.")]
    [RequireSudo]
    public async Task RestartBotAsync(string ipAddressesCommaSeparated)
    {
        var ips = ipAddressesCommaSeparated.Split(',');
        foreach (var ip in ips)
        {
            var bot = SysCord<T>.Runner.GetBot(ip);
            if (bot == null)
            {
                await ReplyAsync($"Kein Bot hat die IP-Adresse ({ip}).").ConfigureAwait(false);
                return;
            }

            var c = bot.Bot.Connection;
            c.Reset();
            bot.Start();
            await Context.Channel.EchoAndReply($"Der Bot auf {ip} ({c.Label}) hat den Befehl zum Neustart erhalten.").ConfigureAwait(false);
        }
    }
}
