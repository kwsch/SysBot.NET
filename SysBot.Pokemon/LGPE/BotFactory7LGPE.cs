using System;
using PKHeX.Core;

namespace SysBot.Pokemon
{
    public sealed class BotFactory7LGPE : BotFactory<PB7>
    {
        public override PokeRoutineExecutorBase CreateBot(PokeTradeHub<PB7> Hub, PokeBotState cfg) => cfg.NextRoutineType switch
        {
            PokeRoutineType.FlexTrade or PokeRoutineType.Idle
                or PokeRoutineType.LinkTrade
                or PokeRoutineType.Clone
                or PokeRoutineType.Dump
                => new PokeTradeBotLGPE(Hub, cfg),

           

            _ => throw new ArgumentException(nameof(cfg.NextRoutineType)),
        };
        public override bool SupportsRoutine(PokeRoutineType type) => type switch
        {
            PokeRoutineType.FlexTrade or PokeRoutineType.Idle
                or PokeRoutineType.LinkTrade
                or PokeRoutineType.Clone
                or PokeRoutineType.Dump
                => true,

            PokeRoutineType.RemoteControl => true,

            _ => false,
        };
    }
}
