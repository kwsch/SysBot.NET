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
            var cfg = SysCordInstance.Self.Hub.Config;
            var channels = ReusableActions.GetListFromString(cfg.GlobalDiscordLoggers);
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
        public async Task AddLogAsync()
        {
            if (!Context.GetIsSudo())
            {
                await ReplyAsync("You are not permitted to use this command.").ConfigureAwait(false);
                return;
            }

            var c = Context.Channel;

            var cid = c.Id;
            if (Channels.TryGetValue(cid, out _))
            {
                await ReplyAsync("Already logging here.").ConfigureAwait(false);
                return;
            }

            AddLogChannel(c, cid);

            // Add to discord global loggers (saves on program close)
            var loggers = ReusableActions.GetListFromString(SysCordInstance.Self.Hub.Config.GlobalDiscordLoggers);
            loggers.Add(cid.ToString());
            SysCordInstance.Self.Hub.Config.GlobalDiscordLoggers = string.Join(", ", new HashSet<string>(loggers));
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
        public async Task DumpLogInfoAsync()
        {
            if (!Context.GetIsSudo())
            {
                await ReplyAsync("You are not permitted to use this command.").ConfigureAwait(false);
                return;
            }

            foreach (var c in Channels)
                await ReplyAsync($"{c.Key} - {c.Value}").ConfigureAwait(false);
        }

        [Command("logClear")]
        [Summary("Clears the logging settings in that specific channel.")]
        public async Task ClearLogsAsync()
        {
            if (!Context.GetIsSudo())
            {
                await ReplyAsync("You are not permitted to use this command.").ConfigureAwait(false);
                return;
            }

            var cfg = SysCordInstance.Self.Hub.Config;
            var channels = cfg.GlobalDiscordLoggers.Split(new[] { ",", ", ", " " }, StringSplitOptions.RemoveEmptyEntries);
            var updatedch = new List<string>();
            foreach (var ch in channels)
            {
                if (!ulong.TryParse(ch, out var cid))
                    continue;
                if (cid != Context.Channel.Id)
                    updatedch.Add(cid.ToString());
                else Channels.Remove(cid);
            }
            SysCordInstance.Self.Hub.Config.GlobalDiscordLoggers = string.Join(", ", updatedch);
            await ReplyAsync($"Logging cleared from channel: {Context.Channel.Name}").ConfigureAwait(false);
        }

        [Command("logClearAll")]
        [Summary("Clears all the logging settings.")]
        public async Task ClearLogsAllAsync()
        {
            if (!Context.GetIsSudo())
            {
                await ReplyAsync("You are not permitted to use this command.").ConfigureAwait(false);
                return;
            }

            foreach (var l in Loggers)
                LogUtil.Forwarders.Remove(l);
            Loggers.Clear();
            Channels.Clear();
            SysCordInstance.Self.Hub.Config.GlobalDiscordLoggers = string.Empty;
            await ReplyAsync("Logging cleared from all channels!").ConfigureAwait(false);
        }
    }
}