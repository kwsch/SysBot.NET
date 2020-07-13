using Google.Apis.YouTube.v3.Data;
using PKHeX.Core;
using StreamingClient.Base.Util;
using SysBot.Base;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using YouTube.Base;
using YouTube.Base.Clients;

namespace SysBot.Pokemon.YouTube
{
    public class YouTubeBot
    {
        private static PokeTradeHub<PK8> Hub;
        internal static TradeQueueInfo<PK8> Info => Hub.Queues.Info;
        private ChatClient client;
        private Channel channel;
        internal static readonly List<YouTubeQueue> QueuePool = new List<YouTubeQueue>();
        private readonly YouTubeSettings Settings;

        public YouTubeBot(YouTubeSettings settings, PokeTradeHub<PK8> hub)
        {
            Hub = hub;
            Settings = settings;
            Logger.LogOccurred += Logger_LogOccurred;

            try
            {
                Task.Run(async () =>
                {
                    var connection = await YouTubeConnection.ConnectViaLocalhostOAuthBrowser(Settings.ClientID, Settings.ClientSecret, Scopes.scopes, true);
                    if (connection != null)
                    {
                        channel = await connection.Channels.GetChannelByID(Settings.ChannelID);
                        if (channel != null)
                        {
                            client = new ChatClient(connection);
                            
                            client.OnMessagesReceived += Client_OnMessagesReceived;
                            if (await client.Connect())
                            {
                                while (true) { }
                            }
                            else
                            {
                            }
                        }
                    }
                    EchoUtil.Forwarders.Add(msg => client.SendMessage(msg));

                });
            }
            catch (Exception ex)
            {
                LogUtil.LogError(ex.Message, "YouTubeBot");
            }
        }

        public void StartingDistribution(string message)
        {
            Task.Run(async () =>
            {
                await client.SendMessage("5...");
                await Task.Delay(1_000).ConfigureAwait(false);
                await client.SendMessage("4...");
                await Task.Delay(1_000).ConfigureAwait(false);
                await client.SendMessage("3...");
                await Task.Delay(1_000).ConfigureAwait(false);
                await client.SendMessage("2...");
                await Task.Delay(1_000).ConfigureAwait(false);
                await client.SendMessage("1...");
                await Task.Delay(1_000).ConfigureAwait(false);
                if (!string.IsNullOrWhiteSpace(message))
                    await client.SendMessage(message);
            });
        }

        private bool AddToTradeQueue(PK8 pk8, int code, LiveChatMessage e, bool sudo, PokeRoutineType type, out string msg)
        {
            // var user = e.WhisperMessage.UserId;
            var userID = ulong.Parse(e.AuthorDetails.ChannelId);
            var name = e.AuthorDetails.DisplayName;

            var trainer = new PokeTradeTrainerInfo(name);
            var notifier = new YouTubeTradeNotifier<PK8>(pk8, trainer, code, e.AuthorDetails.DisplayName, client, Hub.Config.YouTube);
            var tt = type == PokeRoutineType.SeedCheck ? PokeTradeType.Seed : PokeTradeType.Specific;
            var detail = new PokeTradeDetail<PK8>(pk8, trainer, notifier, tt, code: code);
            var trade = new TradeEntry<PK8>(detail, userID, type, name);

            var added = Info.AddToTradeQueue(trade, userID, sudo);

            if (added == QueueResultAdd.AlreadyInQueue)
            {
                msg = $"@{name}: Sorry, you are already in the queue.";
                return false;
            }

            var position = Info.CheckPosition(userID, type);
            msg = $"@{name}: Added to the {type} queue, unique ID: {detail.ID}. Current Position: {position.Position}";

            var botct = Info.Hub.Bots.Count;
            if (position.Position > botct)
            {
                var eta = Info.Hub.Config.Queues.EstimateDelay(position.Position, botct);
                msg += $". Estimated: {eta:F1} minutes.";
            }
            return true;
        }

        private string HandleCommand(LiveChatMessage m, string c, string args)
        {
            bool sudo() => m is LiveChatMessage ch && (ch.AuthorDetails.IsChatOwner.Equals(true) || Settings.IsSudo(m.AuthorDetails.DisplayName));

            switch (c)
            {
                // User Usable Commands
                case "trade":
                    var _ = YouTubeCommandsHelper.AddToWaitingList(args, m.AuthorDetails.DisplayName, m.AuthorDetails.DisplayName, out string msg);
                    return msg;
                case "ts":
                    return $"@{m.AuthorDetails.DisplayName}: {Info.GetPositionString(ulong.Parse(m.AuthorDetails.ChannelId))}";
                case "tc":
                    return $"@{m.AuthorDetails.DisplayName}: {YouTubeCommandsHelper.ClearTrade(ulong.Parse(m.AuthorDetails.ChannelId))}";

                case "code":
                    return YouTubeCommandsHelper.GetCode(ulong.Parse(m.AuthorDetails.ChannelId));

                // Sudo Only Commands
                case "tca" when !sudo():
                case "pr" when !sudo():
                case "pc" when !sudo():
                case "tt" when !sudo():
                case "tcu" when !sudo():
                    return "This command is locked for sudo users only!";

                case "tca":
                    Info.ClearAllQueues();
                    return "Cleared all queues!";

                case "pr":
                    return Info.Hub.Ledy.Pool.Reload() ? $"Reloaded from folder. Pool count: {Info.Hub.Ledy.Pool.Count}" : "Failed to reload from folder.";

                case "pc":
                    return $"The pool count is: {Info.Hub.Ledy.Pool.Count}";

                case "tt":
                    return Info.Hub.Queues.Info.ToggleQueue()
                        ? "Users are now able to join the trade queue."
                        : "Changed queue settings: **Users CANNOT join the queue until it is turned back on.**";

                case "tcu":
                    return YouTubeCommandsHelper.ClearTrade(args);

                default: return null;
            }
        }
        private void Logger_LogOccurred(object sender, Log e)
        {
            LogUtil.LogError(e.Message, "YouTubeBot");
        }

        private void Client_OnMessagesReceived(object sender, IEnumerable<LiveChatMessage> messages)
        {
            foreach (LiveChatMessage message in messages)
            {

                var msg = message.Snippet.TextMessageDetails.MessageText;
                try
                {
                    string[] CommandArgs = message.Snippet.TextMessageDetails.MessageText.Split(' ');
                    string c = CommandArgs[0];
                    string args = CommandArgs[1];

                    var response = HandleCommand(message, c, args);
                    if (response == null)
                        return;
                    client.SendMessage(response);

                }
                catch { }
            }
        }
    }
}
