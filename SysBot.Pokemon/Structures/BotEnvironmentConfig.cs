using SysBot.Base;

namespace SysBot.Pokemon
{
    public class ProgramConfig : BotList<PokeBotConfig>
    {
        public PokeTradeHubConfig Hub { get; set; } = new();
    }
}