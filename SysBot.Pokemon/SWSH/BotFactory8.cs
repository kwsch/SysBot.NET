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

            PokeRoutineType.RaidBot => new RaidBot(cfg, Hub),
            PokeRoutineType.EncounterLine => new EncounterBotLine(cfg, Hub),
            PokeRoutineType.EggFetch => new EncounterBotEgg(cfg, Hub),
            PokeRoutineType.FossilBot => new EncounterBotFossil(cfg, Hub),
            PokeRoutineType.Reset => new EncounterBotReset(cfg, Hub),
            PokeRoutineType.DogBot => new EncounterBotDog(cfg, Hub),

            PokeRoutineType.RemoteControl => new RemoteControlBot(cfg),
            _ => throw new ArgumentException(nameof(cfg.NextRoutineType)),
        };

        public override bool SupportsRoutine(PokeRoutineType type) => type switch
        {
            PokeRoutineType.FlexTrade or PokeRoutineType.Idle
                or PokeRoutineType.SurpriseTrade
                or PokeRoutineType.LinkTrade
                or PokeRoutineType.Clone
                or PokeRoutineType.Dump
                or PokeRoutineType.SeedCheck
                => true,

            PokeRoutineType.RaidBot => true,
            PokeRoutineType.EncounterLine => true,
            PokeRoutineType.EggFetch => true,
            PokeRoutineType.FossilBot => true,
            PokeRoutineType.Reset => true,
            PokeRoutineType.DogBot => true,

            PokeRoutineType.RemoteControl => true,

            _ => false,
        };
    }
}
