using SysBot.Base;

namespace SysBot.Pokemon
{
    public sealed class PokeBotConfig : SwitchBotConfig
    {
        // we need to have the setter public for json serialization
        // ReSharper disable once MemberCanBePrivate.Global
        public PokeRoutineType InitialRoutine { get; set; }
        public PokeRoutineType CurrentRoutineType { get; private set; }
        public PokeRoutineType NextRoutineType { get; private set; }

        public void IterateNextRoutine() => CurrentRoutineType = NextRoutineType;

        /// <summary>
        /// Sets the <see cref="NextRoutineType"/> so that the next iteration will perform as desired,
        /// and updates the <see cref="InitialRoutine"/> in the event the settings are saved.
        /// </summary>
        public void Initialize(PokeRoutineType type)
        {
            NextRoutineType = type;
            InitialRoutine = type;
        }

        public void Initialize() => Resume();
        public void Pause() => NextRoutineType = PokeRoutineType.Idle;
        public void Resume() => NextRoutineType = InitialRoutine;
    }
}
