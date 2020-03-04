using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Discord.Commands;
using Discord.WebSocket;
using PKHeX.Core;
using SysBot.Base;

namespace SysBot.Pokemon.Discord
{
    public class TradeStartModule : ModuleBase<SocketCommandContext>
    {
        private static readonly List<Action<PokeTradeBot, PokeTradeDetail<PK8>>> Loggers = new List<Action<PokeTradeBot, PokeTradeDetail<PK8>>>();
        private static readonly Dictionary<ulong, string> Channels = new Dictionary<ulong, string>();

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

            SysCordInstance.Self.Hub.Queues.Forwarders.Add(Logger);
            static string GetMessage(PokeRoutineExecutor bot, PokeTradeDetail<PK8> detail) => $"> [{DateTime.Now:hh:mm:ss}] - {bot.Connection.Name} is now trading (ID {detail.ID}) {detail.Trainer.TrainerName}";

            Loggers.Add(Logger);
            Channels.Add(cid, c.Name);
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
                else Channels.Remove(cid);
            }
            SysCordInstance.Settings.TradeStartingChannels = string.Join(", ", updatedch);
            await ReplyAsync($"Start Notifications cleared from channel: {Context.Channel.Name}").ConfigureAwait(false);
        }

        [Command("startClearAll")]
        [Summary("Clears all the Start Notification settings.")]
        [RequireSudo]
        public async Task ClearLogsAllAsync()
        {
            foreach (var l in Loggers)
                SysCordInstance.Self.Hub.Queues.Forwarders.Remove(l);
            Loggers.Clear();
            Channels.Clear();
            SysCordInstance.Settings.TradeStartingChannels = string.Empty;
            await ReplyAsync("Start Notifications cleared from all channels!").ConfigureAwait(false);
        }
    }
}