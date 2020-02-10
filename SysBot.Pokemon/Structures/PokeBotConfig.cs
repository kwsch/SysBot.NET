using SysBot.Base;

namespace SysBot.Pokemon
{
    public sealed class PokeBotConfig : SwitchBotConfig
    {
        public PokeRoutineType CurrentRoutineType { get; private set; }
        public PokeRoutineType NextRoutineType { get; set; }

        public void IterateNextRoutine() => CurrentRoutineType = NextRoutineType;
    }
}
