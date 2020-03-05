using System;

namespace SysBot.Pokemon.Discord
{
    public class ChannelAction<T1, T2>
    {
        public readonly ulong ChannelID;
        public readonly string ChannelName;
        public readonly Action<T1, T2> Action;

        public ChannelAction(ulong id, Action<T1, T2> messager, string channel)
        {
            ChannelID = id;
            ChannelName = channel;
            Action = messager;
        }
    }
}