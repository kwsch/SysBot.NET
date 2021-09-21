using System;
using PKHeX.Core;

namespace SysBot.Pokemon
{
    public sealed class BotFactory8 : BotFactory<PK8>
    {
        public override PokeRoutineExecutorBase CreateBot(PokeTradeHub<PK8> Hub, PokeBotState cfg) => cfg.NextRoutineType switch
        {
            PokeRoutineType.FlexTrade or PokeRoutineType.Idle
                or PokeRoutineType.SurpriseTrade
                or PokeRoutineType.LinkTrade
                or PokeRoutineType.Clone
                or PokeRoutineType.Dump
                or PokeRoutineType.SeedCheck
                => new PokeTradeBot(Hub, cfg),

            PokeRoutineType.EggFetch => new EggBot(cfg, Hub),
            PokeRoutineType.FossilBot => new FossilBot(cfg, Hub),
            PokeRoutineType.RaidBot => new RaidBot(cfg, Hub),
            PokeRoutineType.EncounterBot => new EncounterBot(cfg, Hub),
            PokeRoutineType.RemoteControl => new RemoteControlBot(cfg),
            _ => throw new ArgumentException(nameof(cfg.NextRoutineType)),
        };
    }
}
