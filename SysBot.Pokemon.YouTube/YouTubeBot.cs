using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Google.Apis.YouTube.v3.Data;
using PKHeX.Core;
using StreamingClient.Base.Util;
using SysBot.Base;
using YouTube.Base;
using YouTube.Base.Clients;

namespace SysBot.Pokemon.YouTube;

public class YouTubeBot<T> where T : PKM, new()
{
    private ChatClient _client;
    private readonly YouTubeSettings _settings;

    private readonly PokeTradeHub<T> _hub;
    private TradeQueueInfo<T> Info => _hub.Queues.Info;

    public YouTubeBot(YouTubeSettings settings, PokeTradeHub<T> hub)
    {
        _hub = hub;
        _settings = settings;
        Logger.LogOccurred += Logger_LogOccurred;
        _client = null!;

        Task.Run(async () =>
        {
            try
            {
                var connection = await YouTubeConnection.ConnectViaLocalhostOAuthBrowser(_settings.ClientID, _settings.ClientSecret, Scopes.scopes, true).ConfigureAwait(false);
                if (connection == null)
                    return;

                var channel = await connection.Channels.GetChannelByID(_settings.ChannelID).ConfigureAwait(false);
                if (channel == null)
                    return;

                _client = new ChatClient(connection);
                _client.OnMessagesReceived += Client_OnMessagesReceived;
                EchoUtil.Forwarders.Add(msg => _client.SendMessage(msg));

                if (await _client.Connect().ConfigureAwait(false))
                    await Task.Delay(-1).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                LogUtil.LogError(ex.Message, nameof(YouTubeBot<>));
            }
        });
    }

    public void StartingDistribution(string message)
    {
        Task.Run(async () =>
        {
            await _client.SendMessage("5...").ConfigureAwait(false);
            await Task.Delay(1_000).ConfigureAwait(false);
            await _client.SendMessage("4...").ConfigureAwait(false);
            await Task.Delay(1_000).ConfigureAwait(false);
            await _client.SendMessage("3...").ConfigureAwait(false);
            await Task.Delay(1_000).ConfigureAwait(false);
            await _client.SendMessage("2...").ConfigureAwait(false);
            await Task.Delay(1_000).ConfigureAwait(false);
            await _client.SendMessage("1...").ConfigureAwait(false);
            await Task.Delay(1_000).ConfigureAwait(false);
            if (!string.IsNullOrWhiteSpace(message))
                await _client.SendMessage(message).ConfigureAwait(false);
        });
    }

    private string HandleCommand(LiveChatMessage m, string cmd, string args)
    {
        if (!m.AuthorDetails.IsChatOwner.Equals(true) && _settings.IsSudo(m.AuthorDetails.DisplayName))
            return string.Empty; // sudo only commands

        if (args.Length > 0)
            return "Commands don't use arguments. Try again with just the command code.";

        return cmd switch
        {
            "pr" => (Info.Hub.Ledy.Pool.Reload(_hub.Config.Folder.DistributeFolder)
                ? $"Reloaded from folder. Pool count: {Info.Hub.Ledy.Pool.Count}"
                : "Failed to reload from folder."),

            "pc" => $"The pool count is: {Info.Hub.Ledy.Pool.Count}",

            _ => string.Empty,
        };
    }

    private static void Logger_LogOccurred(object? sender, Log e)
    {
        LogUtil.LogError(e.Message, nameof(YouTubeBot<>));
    }

    private void Client_OnMessagesReceived(object? sender, IEnumerable<LiveChatMessage> messages)
    {
        foreach (var message in messages)
        {
            var msg = message.Snippet.TextMessageDetails.MessageText;
            try
            {
                var space = msg.IndexOf(' ');
                if (space < 0)
                    return;

                var cmd = msg[..(space + 1)];
                var args = msg[(space + 1)..];

                var response = HandleCommand(message, cmd, args);
                if (response.Length == 0)
                    return;
                _client.SendMessage(response);
            }
            catch
            {
                // ignored
            }
        }
    }
}
