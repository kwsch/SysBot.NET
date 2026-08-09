using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord.Interactions;
using Discord.WebSocket;
using SysBot.Base;

namespace SysBot.Pokemon.Discord;

[Group("echo", "Control over bot echoes.")]
public class EchoModule : SudoModuleBase
{
    // ReSharper disable NotAccessedPositionalProperty.Local
    private record EchoChannel(ulong ChannelId, string ChannelName, Action<string> Action);
    // ReSharper enable NotAccessedPositionalProperty.Local

    private static readonly Dictionary<ulong, EchoChannel> Channels = [];

    public static void RestoreChannels(DiscordSocketClient discord, DiscordSettings cfg)
    {
        foreach (var ch in cfg.EchoChannels)
        {
            if (discord.GetChannel(ch.ID) is ISocketMessageChannel channel)
                AddEchoChannel(channel, ch.ID);
        }

        EchoUtil.Echo("Added echo notification to Discord channel(s) on Bot startup.");
    }

    [SlashCommand("here", "Makes the bot echo special messages to this channel.")]
    public async Task AddEchoAsync()
    {
        if (!await RequireAsync(CheckSudo(out var e), e).ConfigureAwait(false))
            return;

        if (Context.Interaction.Channel is not { } channel)
        {
            await RespondAsync("This command must be used in a message channel.", ephemeral: true).ConfigureAwait(false);
            return;
        }

        var channelId = channel.Id;
        if (Channels.ContainsKey(channelId))
        {
            await RespondAsync("Already notifying here.").ConfigureAwait(false);
            return;
        }

        AddEchoChannel(channel, channelId);
        SysCordSettings.Settings.EchoChannels.AddIfNew(GetReference(channel));
        await RespondAsync("Added Echo output to this channel!").ConfigureAwait(false);
    }
    private static void AddEchoChannel(ISocketMessageChannel channel, ulong channelId)
    {
        var l = Echo;
        EchoUtil.Forwarders.Add(l);
        Channels.Add(channelId, new EchoChannel(channelId, channel.Name, l));
        return;

        void Echo(string message) => channel.SendMessageAsync(message);
    }

    public static bool IsEchoChannel(ISocketMessageChannel channel) => Channels.ContainsKey(channel.Id);

    [SlashCommand("info", "Dumps the Echo settings.")]
    public async Task DumpEchoInfoAsync()
    {
        if (!await RequireAsync(CheckSudo(out var e), e).ConfigureAwait(false))
            return;

        await RespondAsync(string.Join('\n', Channels.Select(c => $"{c.Key} - {c.Value}"))).ConfigureAwait(false);
    }

    [SlashCommand("clear", "Clears Echo settings from this channel.")]
    public async Task ClearEchosAsync()
    {
        if (!await RequireAsync(CheckSudo(out var e), e).ConfigureAwait(false))
            return;

        var channelId = Context.Interaction.Channel.Id;
        if (!Channels.TryGetValue(channelId, out var echo))
        {
            await RespondAsync("Not echoing in this channel.").ConfigureAwait(false);
            return;
        }

        EchoUtil.Forwarders.Remove(echo.Action);
        Channels.Remove(channelId);
        SysCordSettings.Settings.EchoChannels.RemoveAll(z => z.ID == channelId);
        await RespondAsync($"Echoes cleared from channel: {Context.Interaction.Channel.Name}").ConfigureAwait(false);
    }

    [SlashCommand("clear-all", "Clears all Echo channel settings.")]
    public async Task ClearEchosAllAsync()
    {
        if (!await RequireAsync(CheckSudo(out var e), e).ConfigureAwait(false))
            return;

        foreach (var l in Channels.Values)
            EchoUtil.Forwarders.Remove(l.Action);

        Channels.Clear();
        SysCordSettings.Settings.EchoChannels.Clear();
        await RespondAsync("Echoes cleared from all channels!").ConfigureAwait(false);
    }
}
