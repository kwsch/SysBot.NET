using System;

namespace SysBot.Pokemon.Discord;

public class ChannelAction<T1, T2>(ulong ChannelID, Action<T1, T2> Messager, string ChannelName)
{
    public readonly ulong ChannelID = ChannelID;
    public readonly string ChannelName = ChannelName;
    public readonly Action<T1, T2> Action = Messager;
}
