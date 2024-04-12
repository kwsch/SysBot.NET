using Discord;
using Discord.Commands;
using Discord.WebSocket;
using SysBot.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SysBot.Pokemon.Discord;

public class EchoModule : ModuleBase<SocketCommandContext>
{
    private class EchoChannel(ulong ChannelId, string ChannelName, Action<string> Action)
    {
        public readonly ulong ChannelID = ChannelId;
        public readonly string ChannelName = ChannelName;
        public readonly Action<string> Action = Action;
    }

    private static readonly Dictionary<ulong, EchoChannel> Channels = [];

    public static void RestoreChannels(DiscordSocketClient discord, DiscordSettings cfg)
    {
        foreach (var ch in cfg.EchoChannels)
        {
            if (discord.GetChannel(ch.ID) is ISocketMessageChannel c)
                AddEchoChannel(c, ch.ID);
        }

        EchoUtil.Echo("Echo-Benachrichtigung für Discord-Kanal(e) beim Bot-Start hinzugefügt.");
    }

    [Command("echoHere")]
    [Summary("Ermöglicht das Echo spezieller Nachrichten an den Kanal.")]
    [RequireSudo]
    public async Task AddEchoAsync()
    {
        var c = Context.Channel;
        var cid = c.Id;
        if (Channels.TryGetValue(cid, out _))
        {
            await ReplyAsync("Hier wird bereits gemeldet.").ConfigureAwait(false);
            return;
        }

        AddEchoChannel(c, cid);

        // Add to discord global loggers (saves on program close)
        SysCordSettings.Settings.EchoChannels.AddIfNew(new[] { GetReference(Context.Channel) });
        await ReplyAsync("Echo-Ausgang zu diesem Kanal hinzugefügt!").ConfigureAwait(false);
    }

    private static void AddEchoChannel(ISocketMessageChannel c, ulong cid)
    {
        void Echo(string msg) => c.SendMessageAsync(msg);

        Action<string> l = Echo;
        EchoUtil.Forwarders.Add(l);
        var entry = new EchoChannel(cid, c.Name, l);
        Channels.Add(cid, entry);
    }

    public static bool IsEchoChannel(ISocketMessageChannel c)
    {
        var cid = c.Id;
        return Channels.TryGetValue(cid, out _);
    }

    [Command("echoInfo")]
    [Summary("Gibt die Einstellungen für die Sondermeldung (Echo) aus.")]
    [RequireSudo]
    public async Task DumpEchoInfoAsync()
    {
        foreach (var c in Channels)
            await ReplyAsync($"{c.Key} - {c.Value}").ConfigureAwait(false);
    }

    [Command("echoClear")]
    [Summary("Löscht die speziellen Nachrichtenecho-Einstellungen in diesem speziellen Kanal.")]
    [RequireSudo]
    public async Task ClearEchosAsync()
    {
        var id = Context.Channel.Id;
        if (!Channels.TryGetValue(id, out var echo))
        {
            await ReplyAsync("In diesem Kanal gibt es kein Echo.").ConfigureAwait(false);
            return;
        }
        EchoUtil.Forwarders.Remove(echo.Action);
        Channels.Remove(Context.Channel.Id);
        SysCordSettings.Settings.EchoChannels.RemoveAll(z => z.ID == id);
        await ReplyAsync($"Echos aus dem Kanal entfernt: {Context.Channel.Name}").ConfigureAwait(false);
    }

    [Command("echoClearAll")]
    [Summary("Löscht alle Einstellungen des Echo-Kanals für Sondermeldungen.")]
    [RequireSudo]
    public async Task ClearEchosAllAsync()
    {
        foreach (var l in Channels)
        {
            var entry = l.Value;
            await ReplyAsync($"Echo gelöscht von {entry.ChannelName} ({entry.ChannelID}!").ConfigureAwait(false);
            EchoUtil.Forwarders.Remove(entry.Action);
        }
        EchoUtil.Forwarders.RemoveAll(y => Channels.Select(x => x.Value.Action).Contains(y));
        Channels.Clear();
        SysCordSettings.Settings.EchoChannels.Clear();
        await ReplyAsync("Echos aus allen Kanälen entfernt!").ConfigureAwait(false);
    }

    private RemoteControlAccess GetReference(IChannel channel) => new()
    {
        ID = channel.Id,
        Name = channel.Name,
        Comment = $"Hinzugefügt durch {Context.User.Username} am {DateTime.Now:yyyy.MM.dd-hh:mm:ss}",
    };
}
