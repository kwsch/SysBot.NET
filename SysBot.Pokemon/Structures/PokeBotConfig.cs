using SysBot.Base;

namespace SysBot.Pokemon
{
    public sealed class PokeBotConfig : SwitchBotConfig
    {
        public PokeRoutineType InitialRoutine { get; private set; }
        public PokeRoutineType CurrentRoutineType { get; private set; }
        public PokeRoutineType NextRoutineType { get; private set; }

        public void IterateNextRoutine() => CurrentRoutineType = NextRoutineType;

        public void Initialize(PokeRoutineType type)
        {
            NextRoutineType = type;
            InitialRoutine = type;
        }

        public void Pause() => NextRoutineType = PokeRoutineType.Idle;
        public void Resume() => NextRoutineType = InitialRoutine;
    }
}
