namespace SysBot.Pokemon
{
    public record PokeTradeTrainerInfo
    {
        public readonly string TrainerName;
        public PokeTradeTrainerInfo(string name) => TrainerName = name;
    }
}