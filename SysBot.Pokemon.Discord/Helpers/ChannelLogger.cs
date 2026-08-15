using System;
using System.Runtime.CompilerServices;
using Discord.WebSocket;
using SysBot.Base;

namespace SysBot.Pokemon.Discord;

public sealed record ChannelLogger(ISocketMessageChannel Channel) : ILogForwarder
{
    public void Forward(string message, [CallerMemberName] string identity = "")
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
