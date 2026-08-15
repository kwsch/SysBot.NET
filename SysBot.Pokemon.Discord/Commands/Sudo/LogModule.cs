using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord.Interactions;
using Discord.WebSocket;
using SysBot.Base;

namespace SysBot.Pokemon.Discord;

[Group("log", "Logging commands.")]
public class LogModule : SudoModuleBase
{
    private static readonly Dictionary<ulong, ChannelLogger> Channels=[];
    public static void RestoreLogging(DiscordSocketClient discord, DiscordSettings settings)
    {
        int count = 0;
        foreach (var channelAccess in settings.LoggingChannels)
        {
            if (discord.GetChannel(channelAccess.ID) is not ISocketMessageChannel channel)
            {
                LogUtil.LogInfo($"Failed to add logging to {channelAccess.Name}.");
                continue;
            }

            AddLogChannel(channel, channelAccess.ID);
            count++;
        }

        LogUtil.LogInfo($"Added logging to {count} Discord channel(s) on Bot startup.");
    }

    [SlashCommand("here", "Makes the bot log to this channel.")]
    public async Task AddLogAsync()
    {
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
        SysCordSettings.Settings.LoggingChannels.AddIfNew(GetReference(channel));
        await RespondAsync("Added logging output to this channel!").ConfigureAwait(false);
    }

    private static void AddLogChannel(ISocketMessageChannel channel, ulong channelId)
    {
        var logger = new ChannelLogger(channel);
        LogUtil.Forwarders.Add(logger);
        Channels.Add(channelId, logger);
    }

    [SlashCommand("info", "Dumps the logging settings.")]
    public async Task DumpLogInfoAsync()
    {
        await RespondAsync(string.Join('\n', Channels.Select(c => $"{c.Key} - {c.Value}"))).ConfigureAwait(false);
    }

    [SlashCommand("clear", "Clears logging from this channel.")]
    public async Task ClearLogsAsync()
    {
        var channelId = Context.Interaction.Channel.Id;
        if (!Channels.TryGetValue(channelId, out var log))
        {
            await RespondAsync("Not echoing in this channel.").ConfigureAwait(false);
            return;
        }
        LogUtil.Forwarders.Remove(log);
        Channels.Remove(channelId);
        SysCordSettings.Settings.LoggingChannels.RemoveAll(z => z.ID == channelId);
        await RespondAsync($"Logging cleared from channel: {Context.Interaction.Channel.Name}").ConfigureAwait(false);
    }

    [SlashCommand("clear-all", "Clears all logging settings.")]
    public async Task ClearLogsAllAsync()
    {
        foreach (var l in Channels.Values)
            LogUtil.Forwarders.Remove(l);

        Channels.Clear();
        SysCordSettings.Settings.LoggingChannels.Clear();
        await RespondAsync("Logging cleared from all channels!").ConfigureAwait(false);
    }
}
