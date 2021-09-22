using Discord.Commands;
using Discord.WebSocket;
using PKHeX.Core;
using SysBot.Base;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SysBot.Pokemon.Discord
{
    public class TradeStartModule<T> : ModuleBase<SocketCommandContext> where T : PKM, new()
    {
        private class TradeStartAction : ChannelAction<PokeRoutineExecutorBase, PokeTradeDetail<T>>
        {
            public TradeStartAction(ulong id, Action<PokeRoutineExecutorBase, PokeTradeDetail<T>> messager, string channel) : base(id, messager, channel)
            {
            }
        }

        private static readonly Dictionary<ulong, TradeStartAction> Channels = new();

        private static void Remove(TradeStartAction entry)
        {
            Channels.Remove(entry.ChannelID);
            SysCord<T>.Runner.Hub.Queues.Forwarders.Remove(entry.Action);
        }

#pragma warning disable RCS1158 // Static member in generic type should use a type parameter.
        public static void RestoreTradeStarting(DiscordSocketClient discord)
        {
            var cfg = SysCordSettings.Settings;
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
#pragma warning restore RCS1158 // Static member in generic type should use a type parameter.
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
            var cfg = SysCordSettings.Settings;
            var loggers = ReusableActions.GetListFromString(cfg.TradeStartingChannels);
            loggers.Add(cid.ToString());
            cfg.TradeStartingChannels = string.Join(", ", new HashSet<string>(loggers));
            await ReplyAsync("Added Start Notification output to this channel!").ConfigureAwait(false);
        }

        private static void AddLogChannel(ISocketMessageChannel c, ulong cid)
        {
            void Logger(PokeRoutineExecutorBase bot, PokeTradeDetail<T> detail)
            {
                if (detail.Type == PokeTradeType.Random)
                    return;
                c.SendMessageAsync(GetMessage(bot, detail));
            }

            Action<PokeRoutineExecutorBase, PokeTradeDetail<T>> l = Logger;
            SysCord<T>.Runner.Hub.Queues.Forwarders.Add(l);
            static string GetMessage(PokeRoutineExecutorBase bot, PokeTradeDetail<T> detail) => $"> [{DateTime.Now:hh:mm:ss}] - {bot.Connection.Name} is now trading (ID {detail.ID}) {detail.Trainer.TrainerName}";

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
            var cfg = SysCordSettings.Settings;
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
            cfg.TradeStartingChannels = string.Join(", ", updatedch);
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
                SysCord<T>.Runner.Hub.Queues.Forwarders.Remove(entry.Action);
            }
            Channels.Clear();
            var cfg = SysCordSettings.Settings;
            cfg.TradeStartingChannels = string.Empty;
            await ReplyAsync("Start Notifications cleared from all channels!").ConfigureAwait(false);
        }
    }
}