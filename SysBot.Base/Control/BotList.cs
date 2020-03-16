using System;

namespace SysBot.Base
{
    public class BotList<T> where T : SwitchBotConfig
    {
#pragma warning disable CA1819 // Properties should not return arrays
        public T[] Bots { get; set; } = Array.Empty<T>();
#pragma warning restore CA1819 // Properties should not return arrays
    }
}