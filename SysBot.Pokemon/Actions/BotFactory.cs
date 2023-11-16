using PKHeX.Core;

namespace SysBot.Pokemon;

public abstract class BotFactory<T> where T : PKM, new()
{
    public abstract PokeRoutineExecutorBase CreateBot(PokeTradeHub<T> hub, PokeBotState cfg);
    public abstract bool SupportsRoutine(PokeRoutineType type);
}
