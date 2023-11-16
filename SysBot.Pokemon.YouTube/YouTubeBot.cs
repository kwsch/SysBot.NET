using Google.Apis.YouTube.v3.Data;
using PKHeX.Core;
using StreamingClient.Base.Util;
using SysBot.Base;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using YouTube.Base;
using YouTube.Base.Clients;

namespace SysBot.Pokemon.YouTube;

public class YouTubeBot<T> where T : PKM, new()
{
    private ChatClient client;
    private readonly YouTubeSettings Settings;

    private readonly PokeTradeHub<T> Hub;
    private TradeQueueInfo<T> Info => Hub.Queues.Info;

    public YouTubeBot(YouTubeSettings settings, PokeTradeHub<T> hub)
    {
        Hub = hub;
        Settings = settings;
        Logger.LogOccurred += Logger_LogOccurred;
        client = default!;

        Task.Run(async () =>
        {
            try
            {
                var connection = await YouTubeConnection.ConnectViaLocalhostOAuthBrowser(Settings.ClientID, Settings.ClientSecret, Scopes.scopes, true).ConfigureAwait(false);
                if (connection == null)
                    return;

                var channel = await connection.Channels.GetChannelByID(Settings.ChannelID).ConfigureAwait(false);
                if (channel == null)
                    return;

                client = new ChatClient(connection);
                client.OnMessagesReceived += Client_OnMessagesReceived;
                EchoUtil.Forwarders.Add(msg => client.SendMessage(msg));

                if (await client.Connect().ConfigureAwait(false))
                    await Task.Delay(-1).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                LogUtil.LogError(ex.Message, nameof(YouTubeBot<T>));
            }
        });
    }

    public void StartingDistribution(string message)
    {
        Task.Run(async () =>
        {
            await client.SendMessage("5...").ConfigureAwait(false);
            await Task.Delay(1_000).ConfigureAwait(false);
            await client.SendMessage("4...").ConfigureAwait(false);
            await Task.Delay(1_000).ConfigureAwait(false);
            await client.SendMessage("3...").ConfigureAwait(false);
            await Task.Delay(1_000).ConfigureAwait(false);
            await client.SendMessage("2...").ConfigureAwait(false);
            await Task.Delay(1_000).ConfigureAwait(false);
            await client.SendMessage("1...").ConfigureAwait(false);
            await Task.Delay(1_000).ConfigureAwait(false);
            if (!string.IsNullOrWhiteSpace(message))
                await client.SendMessage(message).ConfigureAwait(false);
        });
    }

    private string HandleCommand(LiveChatMessage m, string cmd, string args)
    {
        if (!m.AuthorDetails.IsChatOwner.Equals(true) && Settings.IsSudo(m.AuthorDetails.DisplayName))
            return string.Empty; // sudo only commands

        if (args.Length > 0)
            return "Commands don't use arguments. Try again with just the command code.";

        return cmd switch
        {
            "pr" => (Info.Hub.Ledy.Pool.Reload(Hub.Config.Folder.DistributeFolder)
                ? $"Reloaded from folder. Pool count: {Info.Hub.Ledy.Pool.Count}"
                : "Failed to reload from folder."),

            "pc" => $"The pool count is: {Info.Hub.Ledy.Pool.Count}",

            _ => string.Empty,
        };
    }

    private static void Logger_LogOccurred(object? sender, Log e)
    {
        LogUtil.LogError(e.Message, nameof(YouTubeBot<T>));
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
                client.SendMessage(response);
            }
            catch
            {
                // ignored
            }
        }
    }
}
