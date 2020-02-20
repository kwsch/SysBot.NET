using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TwitchLib.Client;
using TwitchLib.Client.Events;
using TwitchLib.Client.Models;
using TwitchLib.Communication.Clients;
using TwitchLib.Communication.Models;
using PKHeX.Core;
using PKHeX.Core.AutoMod;

namespace SysBot.Pokemon.Twitch
{
    public class TwitchBot
    {
        internal static readonly TradeQueueInfo<PK8> Info = new TradeQueueInfo<PK8>();
        internal static readonly List<TwitchQueue> QueuePool = new List<TwitchQueue>();
        private readonly TwitchClient client;
        private readonly string Channel;

        public TwitchBot(string username, string token, string channel, PokeTradeHub<PK8> hub)
        {
            var credentials = new ConnectionCredentials(username, token);
            Channel = channel;
            //Hub = hub;
            Info.Hub = hub;
            AutoLegalityExtensions.EnsureInitialized();

            var clientOptions = new ClientOptions
            {
                MessagesAllowedInPeriod = 20,
                ThrottlingPeriod = TimeSpan.FromSeconds(30)
            };

            WebSocketClient customClient = new WebSocketClient(clientOptions);
            client = new TwitchClient(customClient);
            client.Initialize(credentials, channel);

            client.OnLog += Client_OnLog;
            client.OnJoinedChannel += Client_OnJoinedChannel;
            client.OnMessageReceived += Client_OnMessageReceived;
            client.OnWhisperReceived += Client_OnWhisperReceived;
            client.OnNewSubscriber += Client_OnNewSubscriber;
            client.OnConnected += Client_OnConnected;

            client.Connect();
        }

        public void StartingDistribution(string message)
        {
            Task.Run(async () =>
            {
                client.SendMessage(Channel, "5...");
                await Task.Delay(1_000).ConfigureAwait(false);
                client.SendMessage(Channel, "4...");
                await Task.Delay(1_000).ConfigureAwait(false);
                client.SendMessage(Channel, "3...");
                await Task.Delay(1_000).ConfigureAwait(false);
                client.SendMessage(Channel, "2...");
                await Task.Delay(1_000).ConfigureAwait(false);
                client.SendMessage(Channel, "1...");
                await Task.Delay(1_000).ConfigureAwait(false);
                if (!string.IsNullOrWhiteSpace(message))
                    client.SendMessage(Channel, message);
            });
        }

        private bool AddToTradeQueue(PK8 pk8, int code, OnWhisperReceivedArgs e, bool sudo, PokeRoutineType type, out string msg)
        {
            // var user = e.WhisperMessage.UserId;
            var userID = ulong.Parse(e.WhisperMessage.UserId);
            var name = e.WhisperMessage.DisplayName;

            var trainer = new PokeTradeTrainerInfo(name);
            var notifier = new TwitchTradeNotifier<PK8>(pk8, trainer, code, e.WhisperMessage.Username, client, Channel);
            var detail = type == PokeRoutineType.DuduBot ? new PokeTradeDetail<PK8>(pk8, trainer, notifier, PokeTradeType.Dudu, code: code) : new PokeTradeDetail<PK8>(pk8, trainer, notifier, PokeTradeType.Specific, code: code);
            var trade = new TradeEntry<PK8>(detail, userID, type, name);

            var added = Info.AddToTradeQueue(trade, userID, sudo);

            if (added == QueueResultAdd.AlreadyInQueue)
            {
                msg = "Sorry, you are already in the queue.";
                return false;
            }

            msg = $"Added {name} to the queue. Your current position is: {Info.CheckPosition(userID, type).Position}";
            return true;
        }

        private void Client_OnLog(object sender, OnLogArgs e)
        {
            Console.WriteLine($"{DateTime.Now,-19} [{e.BotUsername}] {e.Data}");
        }

        private void Client_OnConnected(object sender, OnConnectedArgs e)
        {
            Console.WriteLine($"{DateTime.Now,-19} [{e.BotUsername}] Connected {e.AutoJoinChannel}");
        }

        private void Client_OnJoinedChannel(object sender, OnJoinedChannelArgs e)
        {
            Console.WriteLine($"{DateTime.Now,-19} [{e.BotUsername}] Joined {e.Channel}");
            client.SendMessage(e.Channel, "Connected!");
        }

        private void Client_OnMessageReceived(object sender, OnMessageReceivedArgs e)
        {
            var command = e.ChatMessage.Message.Split(' ')[0].Trim();
            var p = Info.Hub.Config.DiscordCommandPrefix;
            var channel = e.ChatMessage.Channel;
            bool sudo = TwitchRoleUtil.IsSudo(e.ChatMessage.Username);

            if (!command.StartsWith(p))
                return;

            if (!e.ChatMessage.IsSubscriber && Info.Hub.Config.SubOnlyBot && !sudo)
                return;

            if (command == $"{p}trade")
            {
                var _ = TwitchCommandsHelper.AddToWaitingList(e.ChatMessage.Message.Substring(6).Trim(), e.ChatMessage.DisplayName, e.ChatMessage.Username, out string msg);
                client.SendMessage(channel, msg);
            }

            else if (command == $"{p}tradestatus")
            {
                var msg = TwitchCommandsHelper.GetTradePosition(ulong.Parse(e.ChatMessage.UserId));
                client.SendMessage(channel, msg);
            }

            else if (command == $"{p}tradeclear")
            {
                var msg = TwitchCommandsHelper.ClearTrade(sudo, ulong.Parse(e.ChatMessage.UserId));
                client.SendMessage(channel, msg);
            }

            else if (command == $"{p}tradeclearall")
            {
                // Sudo only
                if (!sudo)
                {
                    client.SendMessage(channel, "This command is locked for sudo users only!");
                    return;
                }
                Info.ClearAllQueues();
                client.SendMessage(channel, "Cleared all queues!");
            }

            else if (command == $"{p}poolreload")
            {
                // Sudo only
                if (!sudo)
                {
                    client.SendMessage(channel, "This command is locked for sudo users only!");
                    return;
                }
                var pool = Info.Hub.Ledy.Pool.Reload();
                if (!pool)
                    client.SendMessage(channel, $"Failed to reload from folder.");
                else
                    client.SendMessage(channel, $"Reloaded from folder. Pool count: {Info.Hub.Ledy.Pool.Count}");
            }

            else if (command == $"{p}poolcount")
            {
                // Sudo only
                if (!sudo)
                {
                    client.SendMessage(channel, "This command is locked for sudo users only!");
                    return;
                }
                client.SendMessage(channel, $"The pool count is: {Info.Hub.Ledy.Pool.Count}");
            }
        }

        private void Client_OnWhisperReceived(object sender, OnWhisperReceivedArgs e)
        {
            if (QueuePool.Count > 100)
            {
                var removed = QueuePool[0];
                QueuePool.RemoveAt(0); // First in, first out
                client.SendMessage(Channel, $"Removed {removed.DisplayName} from the waiting list. (list exceeded maximum count)");
            }

            var user = QueuePool.Find(q => q.UserName == e.WhisperMessage.Username);
            if (user == null)
                return;
            QueuePool.Remove(user);
            var msg = e.WhisperMessage.Message;
            try
            {
                int code = int.Parse(msg);
                var _ = AddToTradeQueue(user.Pokemon, code, e, TwitchRoleUtil.IsSudo(user.UserName), PokeRoutineType.LinkTrade, out string message);
                client.SendMessage(Channel, message);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{ex.Message}");
            }
        }

        private void Client_OnNewSubscriber(object sender, OnNewSubscriberArgs e)
        {
        }
    }
}
