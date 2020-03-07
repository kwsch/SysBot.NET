using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Discord.Commands;
using Discord.WebSocket;
using SysBot.Base;

namespace SysBot.Pokemon.Discord
{
    public class EchoModule : ModuleBase<SocketCommandContext>
    {
        private class EchoChannel
        {
            public readonly ulong ChannelID;
            public readonly string ChannelName;
            public readonly Action<string> Action;

            public EchoChannel(ulong channelId, string channelName, Action<string> action)
            {
                ChannelID = channelId;
                ChannelName = channelName;
                Action = action;
            }
        }

        private static readonly Dictionary<ulong, EchoChannel> Channels = new Dictionary<ulong, EchoChannel>();

        private static void Remove(EchoChannel entry)
        {
            Channels.Remove(entry.ChannelID);
            EchoUtil.Forwarders.Remove(entry.Action);
        }

        public static void RestoreChannels(DiscordSocketClient discord)
        {
            var cfg = SysCordInstance.Settings;
            var channels = ReusableActions.GetListFromString(cfg.EchoChannels);
            foreach (var ch in channels)
            {
                if (!ulong.TryParse(ch, out var cid))
                    continue;
                var c = (ISocketMessageChannel)discord.GetChannel(cid);
                AddEchoChannel(c, cid);
            }

            EchoUtil.Echo("Added echo notification to Discord channel(s) on Bot startup.");
        }

        [Command("echoHere")]
        [Summary("Makes the echo special messages to the channel.")]
        [RequireSudo]
        public async Task AddEchoAsync()
        {
            var c = Context.Channel;
            var cid = c.Id;
            if (Channels.TryGetValue(cid, out _))
            {
                await ReplyAsync("Already start notifying here.").ConfigureAwait(false);
                return;
            }

            AddEchoChannel(c, cid);

            // Add to discord global loggers (saves on program close)
            var loggers = ReusableActions.GetListFromString(SysCordInstance.Settings.EchoChannels);
            loggers.Add(cid.ToString());
            SysCordInstance.Settings.EchoChannels = string.Join(", ", new HashSet<string>(loggers));
            await ReplyAsync("Added Echo output to this channel!").ConfigureAwait(false);
        }

        private static void AddEchoChannel(ISocketMessageChannel c, ulong cid)
        {
            void Echo(string msg) => c.SendMessageAsync(msg);

            Action<string> l = Echo;
            EchoUtil.Forwarders.Add(l);
            var entry = new EchoChannel(cid, c.Name, l);
            Channels.Add(cid, entry);
        }

        [Command("echoInfo")]
        [Summary("Dumps the special message (Echo) settings.")]
        [RequireSudo]
        public async Task DumpEchoInfoAsync()
        {
            foreach (var c in Channels)
                await ReplyAsync($"{c.Key} - {c.Value}").ConfigureAwait(false);
        }

        [Command("echoClear")]
        [Summary("Clears the special message echo settings in that specific channel.")]
        [RequireSudo]
        public async Task ClearEchosAsync()
        {
            var cfg = SysCordInstance.Settings;
            var channels = cfg.EchoChannels.Split(new[] { ",", ", ", " " }, StringSplitOptions.RemoveEmptyEntries);
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
            SysCordInstance.Settings.EchoChannels = string.Join(", ", updatedch);
            await ReplyAsync($"Echoes cleared from channel: {Context.Channel.Name}").ConfigureAwait(false);
        }

        [Command("echoClearAll")]
        [Summary("Clears all the special message Echo channel settings.")]
        [RequireSudo]
        public async Task ClearEchosAllAsync()
        {
            foreach (var l in Channels)
            {
                var entry = l.Value;
                await ReplyAsync($"Echoing cleared from {entry.ChannelName} ({entry.ChannelID}!").ConfigureAwait(false);
                EchoUtil.Forwarders.Remove(entry.Action);
            }
            Channels.Clear();
            SysCordInstance.Settings.EchoChannels = string.Empty;
            await ReplyAsync("Echoes cleared from all channels!").ConfigureAwait(false);
        }
    }
}