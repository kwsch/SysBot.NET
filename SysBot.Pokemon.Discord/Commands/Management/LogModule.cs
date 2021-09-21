using Discord.Commands;
using Discord.WebSocket;
using SysBot.Base;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SysBot.Pokemon.Discord
{
    public class LogModule : ModuleBase<SocketCommandContext>
    {
        private class LogAction : ChannelAction<string, string>
        {
            public LogAction(ulong id, Action<string, string> messager, string channel) : base(id, messager, channel)
            {
            }
        }

        private static readonly Dictionary<ulong, LogAction> Channels = new();

        private static void Remove(LogAction entry)
        {
            Channels.Remove(entry.ChannelID);
            LogUtil.Forwarders.Remove(entry.Action);
        }

        public static void RestoreLogging(DiscordSocketClient discord)
        {
            var settings = SysCordSettings.Settings;
            var channels = ReusableActions.GetListFromString(settings.LoggingChannels);
            foreach (var ch in channels)
            {
                if (!ulong.TryParse(ch, out var cid))
                    continue;
                var c = (ISocketMessageChannel)discord.GetChannel(cid);
                AddLogChannel(c, cid);
            }

            LogUtil.LogInfo("Added logging to Discord channel(s) on Bot startup.", "Discord");
        }

        [Command("logHere")]
        [Summary("Makes the bot log to the channel.")]
        [RequireSudo]
        public async Task AddLogAsync()
        {
            var c = Context.Channel;
            var cid = c.Id;
            if (Channels.TryGetValue(cid, out _))
            {
                await ReplyAsync("Already logging here.").ConfigureAwait(false);
                return;
            }

            AddLogChannel(c, cid);

            // Add to discord global loggers (saves on program close)
            var settings = SysCordSettings.Settings;
            var loggers = ReusableActions.GetListFromString(settings.LoggingChannels);
            loggers.Add(cid.ToString());
            settings.LoggingChannels = string.Join(", ", new HashSet<string>(loggers));
            await ReplyAsync("Added logging output to this channel!").ConfigureAwait(false);
        }

        private static void AddLogChannel(ISocketMessageChannel c, ulong cid)
        {
            void Logger(string msg, string identity)
            {
                try
                {
                    c.SendMessageAsync(GetMessage(msg, identity));
                }
#pragma warning disable CA1031 // Do not catch general exception types
                catch (Exception ex)
#pragma warning restore CA1031 // Do not catch general exception types
                {
                    LogUtil.LogSafe(ex, identity);
                }
            }

            Action<string, string> l = Logger;
            LogUtil.Forwarders.Add(l);
            static string GetMessage(string msg, string identity) => $"> [{DateTime.Now:hh:mm:ss}] - {identity}: {msg}";

            var entry = new LogAction(cid, l, c.Name);
            Channels.Add(cid, entry);
        }

        [Command("logInfo")]
        [Summary("Dumps the logging settings.")]
        [RequireSudo]
        public async Task DumpLogInfoAsync()
        {
            foreach (var c in Channels)
                await ReplyAsync($"{c.Key} - {c.Value}").ConfigureAwait(false);
        }

        [Command("logClear")]
        [Summary("Clears the logging settings in that specific channel.")]
        [RequireSudo]
        public async Task ClearLogsAsync()
        {
            var settings = SysCordSettings.Settings;
            var channels = settings.LoggingChannels.Split(new[] { ",", ", ", " " }, StringSplitOptions.RemoveEmptyEntries);
            var updatedch = new List<string>();
            foreach (var ch in channels)
            {
                if (!ulong.TryParse(ch, out var cid))
                    continue;
                if (cid != Context.Channel.Id)
                    updatedch.Add(cid.ToString());
                else if (Channels.TryGetValue(cid, out var entry))
                    Remove(entry);
            }
            settings.LoggingChannels = string.Join(", ", updatedch);
            await ReplyAsync($"Logging cleared from channel: {Context.Channel.Name}").ConfigureAwait(false);
        }

        [Command("logClearAll")]
        [Summary("Clears all the logging settings.")]
        [RequireSudo]
        public async Task ClearLogsAllAsync()
        {
            foreach (var l in Channels)
            {
                var entry = l.Value;
                await ReplyAsync($"Logging cleared from {entry.ChannelName} ({entry.ChannelID}!").ConfigureAwait(false);
                LogUtil.Forwarders.Remove(entry.Action);
            }
            Channels.Clear();
            var settings = SysCordSettings.Settings;
            settings.LoggingChannels = string.Empty;
            await ReplyAsync("Logging cleared from all channels!").ConfigureAwait(false);
        }
    }
}