using System;

namespace SysBot.Pokemon.WinForms
{
    public sealed class BotEnvironmentConfig
    {
        public PokeTradeHubConfig Hub { get; set; } = new PokeTradeHubConfig();
        public PokeBotConfig[] Bots { get; set; } = Array.Empty<PokeBotConfig>();
    }
}