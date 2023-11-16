using System;

namespace SysBot.Base;

/// <summary>
/// Tracks the state of the bot and what it should execute next.
/// </summary>
[Serializable]
public abstract class BotState<TEnum, TConnection> : IConsoleBotManaged<IConsoleConnection, IConsoleConnectionAsync>
    where TEnum : struct, Enum
    where TConnection : IConsoleBotManaged<IConsoleConnection, IConsoleConnectionAsync>, new()
{
    /// <summary>
    /// Connection Configuration; should always be initialized.
    /// </summary>
    public TConnection Connection { get; set; } = new();

    /// <summary>
    /// Connection Configuration
    /// </summary>
    public IConsoleBotConfig GetInnerConfig() => Connection;

    /// <inheritdoc/>
    public bool IsValid() => Connection.IsValid();

    /// <inheritdoc/>
    public bool Matches(string magic) => Connection.Matches(magic);

    /// <inheritdoc/>
    public IConsoleConnection CreateSync() => Connection.CreateSync();

    /// <inheritdoc/>
    public IConsoleConnectionAsync CreateAsynchronous() => Connection.CreateAsynchronous();

    /// <summary> Advances the internal iteration to begin the next task. </summary>
    public abstract void IterateNextRoutine();

    /// <summary> Method called to first-start the bot. </summary>
    public abstract void Initialize();

    /// <summary> Method called to pause execution of the bot. </summary>
    public abstract void Pause();

    /// <summary> Method called to resume execution of the bot. </summary>
    public abstract void Resume();

    // we need to have the setter public for json serialization
    // ReSharper disable once MemberCanBePrivate.Global

    /// <summary> Routine to start up with. </summary>
    public TEnum InitialRoutine { get; set; }
    /// <summary> Current routine being performed. </summary>
    public TEnum CurrentRoutineType { get; protected set; }
    /// <summary> Next routine to perform. </summary>
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
