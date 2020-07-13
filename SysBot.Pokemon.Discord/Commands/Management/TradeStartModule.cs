using Discord.Commands;
using Discord.WebSocket;
using PKHeX.Core;
using SysBot.Base;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SysBot.Pokemon.Discord
{
    public class TradeStartModule : ModuleBase<SocketCommandContext>
    {
        private class TradeStartAction : ChannelAction<PokeTradeBot, PokeTradeDetail<PK8>>
        {
            public TradeStartAction(ulong id, Action<PokeTradeBot, PokeTradeDetail<PK8>> messager, string channel) : base(id, messager, channel)
            {
            }
        }

        private static readonly Dictionary<ulong, TradeStartAction> Channels = new Dictionary<ulong, TradeStartAction>();

        private static void Remove(TradeStartAction entry)
        {
            Channels.Remove(entry.ChannelID);
            SysCordInstance.Self.Hub.Queues.Forwarders.Remove(entry.Action);
        }

        public static void RestoreTradeStarting(DiscordSocketClient discord)
        {
            var cfg = SysCordInstance.Settings;
            var channels = ReusableActions.GetListFromString(cfg.TradeStartingChannels);
            foreach (var ch in channels)
            {
                if (!ulong.TryParse(ch, out var cid))
                    continue;
                var c = (ISocketMessageChannel)discord.GetChannel(cid);
                AddLogChannel(c, cid);
            }

            LogUtil.LogInfo("Added Start Notification to Discord channel(s) on Bot startup.", "Discord");
        }

        public static bool IsStartChannel(ulong cid)
        {
            return Channels.TryGetValue(cid, out _);
        }

        [Command("startHere")]
        [Summary("Makes the bot log trade starts to the channel.")]
        [RequireSudo]
        public async Task AddLogAsync()
        {
            var c = Context.Channel;
            var cid = c.Id;
            if (Channels.TryGetValue(cid, out _))
            {
                await ReplyAsync("Already start notifying here.").ConfigureAwait(false);
                return;
            }

            AddLogChannel(c, cid);

            // Add to discord global loggers (saves on program close)
            var loggers = ReusableActions.GetListFromString(SysCordInstance.Settings.TradeStartingChannels);
            loggers.Add(cid.ToString());
            SysCordInstance.Settings.TradeStartingChannels = string.Join(", ", new HashSet<string>(loggers));
            await ReplyAsync("Added Start Notification output to this channel!").ConfigureAwait(false);
        }

        private static void AddLogChannel(ISocketMessageChannel c, ulong cid)
        {
            void Logger(PokeTradeBot bot, PokeTradeDetail<PK8> detail)
            {
                if (detail.Type == PokeTradeType.Random)
                    return;
                c.SendMessageAsync(GetMessage(bot, detail));
            }

            Action<PokeTradeBot, PokeTradeDetail<PK8>> l = Logger;
            SysCordInstance.Self.Hub.Queues.Forwarders.Add(l);
            static string GetMessage(PokeRoutineExecutor bot, PokeTradeDetail<PK8> detail) => $"> [{DateTime.Now:hh:mm:ss}] - {bot.Connection.Name} is now trading (ID {detail.ID}) {detail.Trainer.TrainerName}";

            var entry = new TradeStartAction(cid, l, c.Name);
            Channels.Add(cid, entry);
        }

        [Command("startInfo")]
        [Summary("Dumps the Start Notification settings.")]
        [RequireSudo]
        public async Task DumpLogInfoAsync()
        {
            foreach (var c in Channels)
                await ReplyAsync($"{c.Key} - {c.Value}").ConfigureAwait(false);
        }

        [Command("startClear")]
        [Summary("Clears the Start Notification settings in that specific channel.")]
        [RequireSudo]
        public async Task ClearLogsAsync()
        {
            var cfg = SysCordInstance.Settings;
            var channels = cfg.TradeStartingChannels.Split(new[] { ",", ", ", " " }, StringSplitOptions.RemoveEmptyEntries);
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
            SysCordInstance.Settings.TradeStartingChannels = string.Join(", ", updatedch);
            await ReplyAsync($"Start Notifications cleared from channel: {Context.Channel.Name}").ConfigureAwait(false);
        }

        [Command("startClearAll")]
        [Summary("Clears all the Start Notification settings.")]
        [RequireSudo]
        public async Task ClearLogsAllAsync()
        {
            foreach (var l in Channels)
            {
                var entry = l.Value;
                await ReplyAsync($"Logging cleared from {entry.ChannelName} ({entry.ChannelID}!").ConfigureAwait(false);
                SysCordInstance.Self.Hub.Queues.Forwarders.Remove(entry.Action);
            }
            Channels.Clear();
            SysCordInstance.Settings.TradeStartingChannels = string.Empty;
            await ReplyAsync("Start Notifications cleared from all channels!").ConfigureAwait(false);
        }
    }
}