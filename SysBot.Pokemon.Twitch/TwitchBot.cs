using System;
using System.Threading.Tasks;
using TwitchLib.Client;
using TwitchLib.Client.Events;
using TwitchLib.Client.Models;
using TwitchLib.Communication.Clients;
using TwitchLib.Communication.Models;

namespace SysBot.Pokemon.Twitch
{
    public class TwitchBot
    {
        private readonly TwitchClient client;
        private readonly string Channel;

        public TwitchBot(string username, string token, string channel)
        {
            var credentials = new ConnectionCredentials(username, token);
            Channel = channel;

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
                client.SendMessage(Channel, "0...");
                await Task.Delay(1_000).ConfigureAwait(false);
                client.SendMessage(Channel, "Trade now!");
                await Task.Delay(1_000).ConfigureAwait(false);
                if (!string.IsNullOrWhiteSpace(message))
                    client.SendMessage(Channel, message);
            });
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
        }

        private void Client_OnWhisperReceived(object sender, OnWhisperReceivedArgs e)
        {
            client.SendWhisper(e.WhisperMessage.Username, "I don't do anything but reply this message.");
        }

        private void Client_OnNewSubscriber(object sender, OnNewSubscriberArgs e)
        {
        }
    }
}
