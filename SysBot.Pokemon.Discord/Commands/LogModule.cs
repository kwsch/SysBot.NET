using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Discord.Commands;
using SysBot.Base;

namespace SysBot.Pokemon.Discord
{
    public class LogModule : ModuleBase<SocketCommandContext>
    {
        private static readonly List<Action<string, string>> Loggers = new List<Action<string, string>>();
        private static readonly Dictionary<ulong, string> Channels = new Dictionary<ulong, string>();

        [Command("logHere")]
        [Summary("Makes the bot log to the channel.")]
        public async Task AddLogAsync()
        {
            var me = SysCordInstance.Self;
            var hub = me.Hub;
            if (!Context.GetHasRole(hub.Config.DiscordRoleSudo))
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

            void Logger(string msg, string identity) => c.SendMessageAsync(GetMessage(msg, identity));
            LogUtil.Forwarders.Add(Logger);
            static string GetMessage(string msg, string identity) => $"> [{DateTime.Now:hh:mm:ss}] - {identity}: {msg}";
            await ReplyAsync("Added logging output to this channel!").ConfigureAwait(false);

            Loggers.Add(Logger);
            Channels.Add(cid, c.Name);
        }

        [Command("logInfo")]
        [Summary("Dumps the logging settings.")]
        public async Task DumpLogInfoAsync()
        {
            var me = SysCordInstance.Self;
            var hub = me.Hub;
            if (!Context.GetHasRole(hub.Config.DiscordRoleSudo))
            {
                await ReplyAsync("You are not permitted to use this command.").ConfigureAwait(false);
                return;
            }

            foreach (var c in Channels)
                await ReplyAsync($"{c.Key} - {c.Value}").ConfigureAwait(false);
        }

        [Command("logClear")]
        [Summary("Clears the logging settings.")]
        public async Task ClearLogsAsync()
        {
            var me = SysCordInstance.Self;
            var hub = me.Hub;
            if (!Context.GetHasRole(hub.Config.DiscordRoleSudo))
            {
                await ReplyAsync("You are not permitted to use this command.").ConfigureAwait(false);
                return;
            }

            foreach (var l in Loggers)
                LogUtil.Forwarders.Remove(l);
            Loggers.Clear();
            Channels.Clear();
        }
    }
}