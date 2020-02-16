namespace SysBot.Pokemon
{
    public class ProgramConfig : BotList
    {
        public PokeTradeHubConfig Hub { get; set; } = new PokeTradeHubConfig();
    }
}