using PKHeX.Core;
using SysBot.Base;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TwitchLib.Client;
using TwitchLib.Client.Events;
using TwitchLib.Client.Models;
using TwitchLib.Communication.Clients;
using TwitchLib.Communication.Models;

namespace SysBot.Pokemon.Twitch;

public class TwitchBot<T> where T : PKM, new()
{
    private static PokeTradeHub<T> Hub = null!;
    internal static TradeQueueInfo<T> Info => Hub.Queues.Info;

    internal static readonly List<TwitchQueue<T>> QueuePool = [];
    private readonly TwitchClient client;
    private readonly string Channel;
    private readonly TwitchSettings Settings;

    public TwitchBot(TwitchSettings settings, PokeTradeHub<T> hub)
    {
        Hub = hub;
        Settings = settings;

        var credentials = new ConnectionCredentials(settings.Username.ToLower(), settings.Token);

        var clientOptions = new ClientOptions
        {
            // message queue capacity is managed (10_000 for message & whisper separately)
            // message send interval is managed (50ms for each message sent)
        };

        Channel = settings.Channel;
        WebSocketClient customClient = new(clientOptions);
        client = new TwitchClient(customClient);

        client.Initialize(credentials, Channel);

        client.OnJoinedChannel += Client_OnJoinedChannel;
        client.OnMessageReceived += Client_OnMessageReceived;
        client.OnWhisperReceived += Client_OnWhisperReceived;
        client.OnChatCommandReceived += Client_OnChatCommandReceived;
        client.OnWhisperCommandReceived += Client_OnWhisperCommandReceived;
        client.OnConnected += Client_OnConnected;
        client.OnDisconnected += Client_OnDisconnected;
        client.OnLeftChannel += Client_OnLeftChannel;

        client.OnMessageSent += async (_, e) =>
        {
            LogUtil.LogText($"[{client.TwitchUsername}] - Message Sent in {e.SentMessage.Channel}: {e.SentMessage.Message}");
            await Task.CompletedTask;
        };

        client.OnMessageThrottled += async (_, e) =>
        {
            LogUtil.LogError($"Message Throttled: {e}", "TwitchBot");
            await Task.CompletedTask;
        };

        client.OnError += (_, e) =>
        {
            LogUtil.LogError(e.Exception.Message + Environment.NewLine + e.Exception.StackTrace, "TwitchBot");
            return Task.CompletedTask;
        };
        client.OnConnectionError += (_, e) =>
        {
            LogUtil.LogError(e.BotUsername + Environment.NewLine + e.Error.Message, "TwitchBot");
            return Task.CompletedTask;
        };

        _ = client.ConnectAsync();

        EchoUtil.Forwarders.Add(msg => _ = client.SendMessageAsync(Channel, msg, false));

        // Turn on if verified
        // Hub.Queues.Forwarders.Add((bot, detail) => client.SendMessage(Channel, $"{bot.Connection.Name} is now trading (ID {detail.ID}) {detail.Trainer.TrainerName}"));
    }

    public void StartingDistribution(string message)
    {
        Task.Run(async () =>
        {
            await client.SendMessageAsync(Channel, "5...", false).ConfigureAwait(false);
            await Task.Delay(1_000).ConfigureAwait(false);
            await client.SendMessageAsync(Channel, "4...", false).ConfigureAwait(false);
            await Task.Delay(1_000).ConfigureAwait(false);
            await client.SendMessageAsync(Channel, "3...", false).ConfigureAwait(false);
            await Task.Delay(1_000).ConfigureAwait(false);
            await client.SendMessageAsync(Channel, "2...", false).ConfigureAwait(false);
            await Task.Delay(1_000).ConfigureAwait(false);
            await client.SendMessageAsync(Channel, "1...", false).ConfigureAwait(false);
            await Task.Delay(1_000).ConfigureAwait(false);
            if (!string.IsNullOrWhiteSpace(message))
                await client.SendMessageAsync(Channel, message, false).ConfigureAwait(false);
        });
    }

    private bool AddToTradeQueue(T pk, int code, OnWhisperReceivedArgs e, RequestSignificance sig, PokeRoutineType type, out string msg)
    {
        // var user = e.WhisperMessage.UserId;
        var userID = ulong.Parse(e.WhisperMessage.UserId);
        var name = e.WhisperMessage.DisplayName;

        var trainer = new PokeTradeTrainerInfo(name, ulong.Parse(e.WhisperMessage.UserId));
        var notifier = new TwitchTradeNotifier<T>(pk, trainer, code, e.WhisperMessage.Username, client, Channel, Hub.Config.Twitch);
        var tt = type == PokeRoutineType.SeedCheck ? PokeTradeType.Seed : PokeTradeType.Specific;
        var detail = new PokeTradeDetail<T>(pk, trainer, notifier, tt, code, sig == RequestSignificance.Favored);
        var trade = new TradeEntry<T>(detail, userID, type, name);

        var added = Info.AddToTradeQueue(trade, userID, sig == RequestSignificance.Owner);

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

    private Task Client_OnConnected(object? sender, OnConnectedEventArgs e)
    {
        LogUtil.LogText($"[{client.TwitchUsername}] - Connected as {e.BotUsername}");
        return Task.CompletedTask;
    }

    private async Task Client_OnDisconnected(object? sender, OnDisconnectedArgs e)
    {
        LogUtil.LogText($"[{client.TwitchUsername}] - Disconnected.");
        while (!client.IsConnected)
        {
            await client.ReconnectAsync().ConfigureAwait(false);
            await Task.Delay(1_000).ConfigureAwait(false);
        }
    }

    private async Task Client_OnJoinedChannel(object? sender, OnJoinedChannelArgs e)
    {
        LogUtil.LogInfo($"Joined {e.Channel}", e.BotUsername);
        await client.SendMessageAsync(e.Channel, "Connected!", false).ConfigureAwait(false);
    }

    private async Task Client_OnMessageReceived(object? sender, OnMessageReceivedArgs e)
    {
        LogUtil.LogText($"[{client.TwitchUsername}] - Received message: @{e.ChatMessage.Username}: {e.ChatMessage.Message}");
        if (client.JoinedChannels.Count == 0)
            await client.JoinChannelAsync(e.ChatMessage.Channel, false).ConfigureAwait(false);
    }

    private async Task Client_OnLeftChannel(object? sender, OnLeftChannelArgs e)
    {
        LogUtil.LogText($"[{client.TwitchUsername}] - Left channel {e.Channel}");
        await client.JoinChannelAsync(e.Channel, false).ConfigureAwait(false);
    }

    private async Task Client_OnChatCommandReceived(object? sender, OnChatCommandReceivedArgs e)
    {
        if (!Hub.Config.Twitch.AllowCommandsViaChannel || Hub.Config.Twitch.UserBlacklist.Contains(e.ChatMessage.Username))
            return;

        var msg = e.ChatMessage;
        var c = e.Command.Name.ToLower();
        var args = e.Command.ArgumentsAsString;
        var response = HandleCommand(msg, c, args, false);
        if (response.Length == 0)
            return;

        var channel = e.ChatMessage.Channel;
        await client.SendMessageAsync(channel, response, false).ConfigureAwait(false);
    }

    private async Task Client_OnWhisperCommandReceived(object? sender, OnWhisperCommandReceivedArgs e)
    {
        if (!Hub.Config.Twitch.AllowCommandsViaWhisper || Hub.Config.Twitch.UserBlacklist.Contains(e.WhisperMessage.Username))
            return;

        var msg = e.WhisperMessage;
        var c = e.Command.Name.ToLower();
        var args = e.Command.ArgumentsAsString;
        var response = HandleCommand(msg, c, args, true);
        if (response.Length == 0)
            return;

        await client.SendMessageAsync(Channel, $"/w {msg.Username} {response}", false).ConfigureAwait(false);
    }

    private string HandleCommand(TwitchLibMessage m, string c, string args, bool whisper)
    {
        bool sudo() => m is ChatMessage ch && (ch.IsBroadcaster || Settings.IsSudo(m.Username));
        bool subscriber() => m is ChatMessage { SubscribedMonthCount: > 0 };

        switch (c)
        {
            // User Usable Commands
            case "trade":
                var _ = TwitchCommandsHelper<T>.AddToWaitingList(args, m.DisplayName, m.Username, ulong.Parse(m.UserId), subscriber(), out string msg);
                return msg;
            case "ts":
                return $"@{m.Username}: {Info.GetPositionString(ulong.Parse(m.UserId))}";
            case "tc":
                return $"@{m.Username}: {TwitchCommandsHelper<T>.ClearTrade(ulong.Parse(m.UserId))}";

            case "code" when whisper:
                return TwitchCommandsHelper<T>.GetCode(ulong.Parse(m.UserId));

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
                return Info.Hub.Ledy.Pool.Reload(Hub.Config.Folder.DistributeFolder) ? $"Reloaded from folder. Pool count: {Info.Hub.Ledy.Pool.Count}" : "Failed to reload from folder.";

            case "pc":
                return $"The pool count is: {Info.Hub.Ledy.Pool.Count}";

            case "tt":
                return Info.Hub.Queues.Info.ToggleQueue()
                    ? "Users are now able to join the trade queue."
                    : "Changed queue settings: **Users CANNOT join the queue until it is turned back on.**";

            case "tcu":
                return TwitchCommandsHelper<T>.ClearTrade(args);

            default: return string.Empty;
        }
    }

    private async Task Client_OnWhisperReceived(object? sender, OnWhisperReceivedArgs e)
    {
        LogUtil.LogText($"[{client.TwitchUsername}] - @{e.WhisperMessage.Username}: {e.WhisperMessage.Message}");
        if (QueuePool.Count > 100)
        {
            var removed = QueuePool[0];
            QueuePool.RemoveAt(0); // First in, first out
            await client.SendMessageAsync(Channel, $"Removed @{removed.DisplayName} ({(Species)removed.Entity.Species}) from the waiting list: stale request.", false).ConfigureAwait(false);
        }

        var user = QueuePool.FindLast(q => q.UserName == e.WhisperMessage.Username);
        if (user == null)
            return;
        QueuePool.Remove(user);
        var msg = e.WhisperMessage.Message;
        try
        {
            int code = Util.ToInt32(msg);
            var sig = GetUserSignificance(user);
            _ = AddToTradeQueue(user.Entity, code, e, sig, PokeRoutineType.LinkTrade, out string message);
            await client.SendMessageAsync(Channel, message, false).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            LogUtil.LogSafe(ex, nameof(TwitchBot<T>));
            LogUtil.LogError($"{ex.Message}", nameof(TwitchBot<T>));
        }
    }

    private RequestSignificance GetUserSignificance(TwitchQueue<T> user)
    {
        var name = user.UserName;
        if (name == Channel)
            return RequestSignificance.Owner;
        if (Settings.IsSudo(user.UserName))
            return RequestSignificance.Favored;
        return user.IsSubscriber ? RequestSignificance.Favored : RequestSignificance.None;
    }
}
