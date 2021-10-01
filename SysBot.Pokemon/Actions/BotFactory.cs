using System;
using PKHeX.Core;

namespace SysBot.Pokemon
{
    public abstract class BotFactory<T> where T : PKM, new()
    {
        public virtual PokeRoutineExecutorBase CreateBot(PokeTradeHub<T> hub, PokeBotState cfg) => throw new Exception();
    }
}
