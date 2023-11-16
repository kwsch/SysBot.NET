namespace SysBot.Pokemon;

public record PokeTradeTrainerInfo
{
    public readonly string TrainerName;
    public readonly ulong ID;

    public PokeTradeTrainerInfo(string name, ulong id = 0)
    {
        TrainerName = name;
        ID = id;
    }
}
