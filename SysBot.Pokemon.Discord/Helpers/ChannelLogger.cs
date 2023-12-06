using System;
using Discord.WebSocket;
using SysBot.Base;

namespace SysBot.Pokemon.Discord;

public class ChannelLogger(ulong ChannelID, ISocketMessageChannel Channel) : ILogForwarder
{
    public ulong ChannelID { get; } = ChannelID;
    public string ChannelName => Channel.Name;

    public void Forward(string message, string identity)
    {
        try
        {
            var text = GetMessage(message, identity);
            Channel.SendMessageAsync(text);
        }
        catch (Exception ex)
        {
            LogUtil.LogSafe(ex, identity);
        }
    }
    private static string GetMessage(ReadOnlySpan<char> msg, string identity)
        => $"> [{DateTime.Now:hh:mm:ss}] - {identity}: {msg}";
}
