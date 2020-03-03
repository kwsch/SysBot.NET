using System;

namespace SysBot.Pokemon
{
    public class BotList
    {
#pragma warning disable CA1819 // Properties should not return arrays
        public PokeBotConfig[] Bots { get; set; } = Array.Empty<PokeBotConfig>();
#pragma warning restore CA1819 // Properties should not return arrays
    }
}