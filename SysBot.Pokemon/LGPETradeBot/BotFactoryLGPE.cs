using PKHeX.Core;
using System;

namespace SysBot.Pokemon;

public sealed class BotFactoryLGPE : BotFactory<PB7>
{
    public override PokeRoutineExecutorBase CreateBot(PokeTradeHub<PB7> Hub, PokeBotState cfg)
    {
        return cfg.NextRoutineType switch
        {
                PokeRoutineType.Idle
                or PokeRoutineType.FlexTrade
                => new LetsGoTrades(Hub, cfg),

            _ => throw new ArgumentException(nameof(cfg.NextRoutineType)),
        };
    }

    public override bool SupportsRoutine(PokeRoutineType type) => type switch
    {
        PokeRoutineType.FlexTrade or PokeRoutineType.Idle
            => true,

        _ => false,
    };
}
