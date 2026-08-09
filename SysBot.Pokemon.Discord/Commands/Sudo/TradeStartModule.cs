using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord.Interactions;
using Discord.WebSocket;
using PKHeX.Core;
using SysBot.Base;

namespace SysBot.Pokemon.Discord;

[Group("start", "Trade Start notification command settings.")]
public class TradeStartModule<T> : SudoModuleBase where T : PKM, new()
{
    private record TradeStartAction(ulong ChannelId, Action<PokeRoutineExecutorBase, PokeTradeDetail<T>> Messager, string ChannelName)
        : ChannelAction<PokeRoutineExecutorBase, PokeTradeDetail<T>>(ChannelId, Messager, ChannelName);

    private static readonly Dictionary<ulong, TradeStartAction> Channels = [];
    public static bool IsStartChannel(ulong channelId) => Channels.ContainsKey(channelId);

    private static void Remove(TradeStartAction e)
    {
        Channels.Remove(e.ChannelId);
        SysCord<T>.Runner.Hub.Queues.Forwarders.Remove(e.Messager);
    }

    public static void RestoreTradeStarting(DiscordSocketClient discord, DiscordSettings settings)
    {
        int count = 0;
        foreach (var channelAccess in settings.TradeStartingChannels)
        {
            if (discord.GetChannel(channelAccess.ID) is not ISocketMessageChannel channel)
            {
                LogUtil.LogInfo($"Failed to add logging to {channelAccess.Name}.");
                continue;
            }

            AddLogChannel(channel, channelAccess.ID);
            count++;
        }

        LogUtil.LogInfo($"Added Trade Start Notification to {count} Discord channel(s) on Bot startup.");
    }

    [SlashCommand("here", "Makes the bot log trade starts to this channel.")]
    public async Task AddLogAsync()
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
            await RespondAsync("Already logging here.").ConfigureAwait(false);
            return;
        }

        AddLogChannel(channel, channelId);
        SysCordSettings.Settings.TradeStartingChannels.AddIfNew(GetReference(channel));
        await RespondAsync("Added Start Notification output to this channel!").ConfigureAwait(false);
    }

    private static void AddLogChannel(ISocketMessageChannel c, ulong channelId)
    {
        var l = Logger;
        SysCord<T>.Runner.Hub.Queues.Forwarders.Add(l);
        Channels.Add(channelId, new TradeStartAction(channelId, l, c.Name));
        return;

        void Logger(PokeRoutineExecutorBase bot, PokeTradeDetail<T> detail)
        {
            if (detail.Type != PokeTradeType.Random)
                _ = c.SendMessageAsync($"> [{DateTime.Now:hh:mm:ss}] - {bot.Connection.Label} is now trading (ID {detail.Id}) {detail.Trainer.TrainerName}");
        }
    }

    [SlashCommand("info", "Dumps the Start Notification settings.")]
    public async Task DumpLogInfoAsync()
    {
        if (!await RequireAsync(CheckSudo(out var e), e).ConfigureAwait(false))
            return;

        await RespondAsync(string.Join('\n', Channels.Select(c => $"{c.Key} - {c.Value}"))).ConfigureAwait(false);
    }

    [SlashCommand("clear", "Clears Start Notification settings from this channel.")]
    public async Task ClearLogsAsync()
    {
        if (!await RequireAsync(CheckSudo(out var e), e).ConfigureAwait(false))
            return;

        var id = Context.Interaction.Channel.Id;
        if (Channels.TryGetValue(id, out var entry))
            Remove(entry);
        SysCordSettings.Settings.TradeStartingChannels.RemoveAll(z => z.ID == id);
        await RespondAsync($"Start Notifications cleared from channel: {Context.Interaction.Channel.Name}").ConfigureAwait(false);
    }

    [SlashCommand("clear-all", "Clears all Start Notification settings.")]
    public async Task ClearLogsAllAsync()
    {
        if (!await RequireAsync(CheckSudo(out var e), e).ConfigureAwait(false))
            return;

        foreach (var entry in Channels.Values)
            SysCord<T>.Runner.Hub.Queues.Forwarders.Remove(entry.Messager);

        Channels.Clear();
        SysCordSettings.Settings.TradeStartingChannels.Clear();
        await RespondAsync("Start Notifications cleared from all channels!").ConfigureAwait(false);
    }
}
