using System;
using SysBot.Base;

namespace SysBot.Pokemon
{
    /// <summary>
    /// Tracks the state of the bot and what it should execute next.
    /// </summary>
    [Serializable]
    public sealed class PokeBotConfig : IConsoleBotManaged<IConsoleConnection, IConsoleConnectionAsync>
    {
        public IConsoleBotManaged<IConsoleConnection, IConsoleConnectionAsync> Connection { get; set; } = new SwitchConnectionConfig();
        public bool IsValid() => Connection.IsValid();
        public bool Matches(string magic) => Connection.Matches(magic);

        public IConsoleConnection CreateSync() => Connection.CreateSync();
        public IConsoleConnectionAsync CreateAsynchronous() => Connection.CreateAsynchronous();

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
