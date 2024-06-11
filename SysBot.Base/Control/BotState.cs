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

    /// <summary> Current routine being performed. </summary>
    public TEnum CurrentRoutineType { get; protected set; }

    /// <summary> Routine to start up with. </summary>
    public TEnum InitialRoutine { get; set; }

    // we need to have the setter public for json serialization
    // ReSharper disable once MemberCanBePrivate.Global
    /// <summary> Next routine to perform. </summary>
    public TEnum NextRoutineType { get; protected set; }

    /// <inheritdoc/>
    public IConsoleConnectionAsync CreateAsynchronous() => Connection.CreateAsynchronous();

    /// <inheritdoc/>
    public IConsoleConnection CreateSync() => Connection.CreateSync();

    /// <summary>
    /// Connection Configuration
    /// </summary>
    public IConsoleBotConfig GetInnerConfig() => Connection;

    /// <summary> Method called to first-start the bot. </summary>
    public abstract void Initialize();

    /// <summary>
    /// Sets the <see cref="NextRoutineType"/> so that the next iteration will perform as desired,
    /// and updates the <see cref="InitialRoutine"/> in the event the settings are saved.
    /// </summary>
    public void Initialize(TEnum type)
    {
        NextRoutineType = type;
        InitialRoutine = type;
    }

    /// <inheritdoc/>
    public bool IsValid() => Connection.IsValid();

    /// <summary> Advances the internal iteration to begin the next task. </summary>
    public abstract void IterateNextRoutine();

    /// <inheritdoc/>
    public bool Matches(string magic) => Connection.Matches(magic);

    /// <summary> Method called to pause execution of the bot. </summary>
    public abstract void Pause();

    /// <summary> Method called to resume execution of the bot. </summary>
    public abstract void Resume();
}
