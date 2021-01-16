using System;

namespace SysBot.Base
{
    /// <summary>
    /// Tracks the state of the bot and what it should execute next.
    /// </summary>
    [Serializable]
    public abstract class BotState<TEnum, TConnection> : IConsoleBotManaged<IConsoleConnection, IConsoleConnectionAsync>
        where TEnum : struct, Enum
        where TConnection : IConsoleBotManaged<IConsoleConnection, IConsoleConnectionAsync>
    {
        public TConnection Connection { get; set; } = default!;
        public bool IsValid() => Connection.IsValid();
        public bool Matches(string magic) => Connection.Matches(magic);

        public IConsoleConnection CreateSync() => Connection.CreateSync();
        public IConsoleConnectionAsync CreateAsynchronous() => Connection.CreateAsynchronous();

        public abstract void IterateNextRoutine();
        public abstract void Initialize();
        public abstract void Pause();
        public abstract void Resume();

        // we need to have the setter public for json serialization
        // ReSharper disable once MemberCanBePrivate.Global
        public TEnum InitialRoutine { get; set; }
        public TEnum CurrentRoutineType { get; protected set; }
        public TEnum NextRoutineType { get; protected set; }

        /// <summary>
        /// Sets the <see cref="NextRoutineType"/> so that the next iteration will perform as desired,
        /// and updates the <see cref="InitialRoutine"/> in the event the settings are saved.
        /// </summary>
        public void Initialize(TEnum type)
        {
            NextRoutineType = type;
            InitialRoutine = type;
        }
    }
}
