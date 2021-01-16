using System;
using SysBot.Base;

namespace SysBot.Pokemon
{
    /// <summary>
    /// Tracks the state of the bot and what it should execute next.
    /// </summary>
    [Serializable]
    public sealed class PokeBotState : BotState<PokeRoutineType, SwitchConnectionConfig>
    {
        public override void IterateNextRoutine() => CurrentRoutineType = NextRoutineType;
        public override void Initialize() => Resume();
        public override void Pause() => NextRoutineType = PokeRoutineType.Idle;
        public override void Resume() => NextRoutineType = InitialRoutine;
    }
}
