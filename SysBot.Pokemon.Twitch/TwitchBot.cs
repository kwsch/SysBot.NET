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
        private static readonly List<TwitchQueue> QueuePool = new List<TwitchQueue>();
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
            var userID = ulong.MinValue;
            var name = e.WhisperMessage.DisplayName;

            var trainer = new PokeTradeTrainerInfo(name);
            var notifier = new TwitchTradeNotifier<PK8>(pk8, trainer, code, e.WhisperMessage.Username, client, Channel);
            var detail = new PokeTradeDetail<PK8>(pk8, trainer, notifier, PokeTradeType.Specific, code: code);
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
            if (e.ChatMessage.Message.StartsWith($"{Info.Hub.Config.DiscordCommandPrefix}trade"))
            {
                var setstring = e.ChatMessage.Message.Substring(6).Trim();
                ShowdownSet set = TwitchShowdownUtil.ConvertToShowdown(setstring);
                var sav = TrainerSettings.GetSavedTrainerData(8);
                PKM pkm = sav.GetLegalFromSet(set, out _);
                if (new LegalityAnalysis(pkm).Valid && pkm is PK8 p8)
                {
                    var tq = new TwitchQueue(p8, new PokeTradeTrainerInfo(e.ChatMessage.DisplayName),
                        e.ChatMessage.Username);
                    QueuePool.Add(tq);
                    client.SendMessage(e.ChatMessage.Channel, "Added you to the waiting list. Please whisper to me your trade code! Your request from the waiting list will be removed if you are too slow!");
                }
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
