using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Discord.Commands;
using Discord.WebSocket;
using SysBot.Base;

namespace SysBot.Pokemon.Discord
{
    public class LogModule : ModuleBase<SocketCommandContext>
    {
        private static readonly List<Action<string, string>> Loggers = new List<Action<string, string>>();
        private static readonly Dictionary<ulong, string> Channels = new Dictionary<ulong, string>();

        public static void RestoreLogging(DiscordSocketClient discord)
        {
            var cfg = SysCordInstance.Settings;
            var channels = ReusableActions.GetListFromString(cfg.LoggingChannels);
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
            var loggers = ReusableActions.GetListFromString(SysCordInstance.Settings.LoggingChannels);
            loggers.Add(cid.ToString());
            SysCordInstance.Settings.LoggingChannels = string.Join(", ", new HashSet<string>(loggers));
            await ReplyAsync("Added logging output to this channel!").ConfigureAwait(false);
        }

        private static void AddLogChannel(ISocketMessageChannel c, ulong cid)
        {
            void Logger(string msg, string identity) => c.SendMessageAsync(GetMessage(msg, identity));
            LogUtil.Forwarders.Add(Logger);
            static string GetMessage(string msg, string identity) => $"> [{DateTime.Now:hh:mm:ss}] - {identity}: {msg}";

            Loggers.Add(Logger);
            Channels.Add(cid, c.Name);
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
            var cfg = SysCordInstance.Settings;
            var channels = cfg.LoggingChannels.Split(new[] { ",", ", ", " " }, StringSplitOptions.RemoveEmptyEntries);
            var updatedch = new List<string>();
            foreach (var ch in channels)
            {
                if (!ulong.TryParse(ch, out var cid))
                    continue;
                if (cid != Context.Channel.Id)
                    updatedch.Add(cid.ToString());
                else Channels.Remove(cid);
            }
            SysCordInstance.Settings.LoggingChannels = string.Join(", ", updatedch);
            await ReplyAsync($"Logging cleared from channel: {Context.Channel.Name}").ConfigureAwait(false);
        }

        [Command("logClearAll")]
        [Summary("Clears all the logging settings.")]
        [RequireSudo]
        public async Task ClearLogsAllAsync()
        {
            foreach (var l in Loggers)
                LogUtil.Forwarders.Remove(l);
            Loggers.Clear();
            Channels.Clear();
            SysCordInstance.Settings.LoggingChannels = string.Empty;
            await ReplyAsync("Logging cleared from all channels!").ConfigureAwait(false);
        }
    }
}