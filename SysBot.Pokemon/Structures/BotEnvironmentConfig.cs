using SysBot.Base;

namespace SysBot.Pokemon
{
    public class ProgramConfig : BotList<PokeBotState>
    {
        public PokeTradeHubConfig Hub { get; set; } = new();
    }
}