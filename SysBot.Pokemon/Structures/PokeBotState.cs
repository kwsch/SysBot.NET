using SysBot.Base;
using System;

namespace SysBot.Pokemon;

/// <summary>
/// Tracks the state of the bot and what it should execute next.
/// </summary>
[Serializable]
public sealed class PokeBotState : BotState<PokeRoutineType, SwitchConnectionConfig>
{
    /// <inheritdoc/>
    public override void IterateNextRoutine() => CurrentRoutineType = NextRoutineType;
    /// <inheritdoc/>
    public override void Initialize() => Resume();
    /// <inheritdoc/>
    public override void Pause() => NextRoutineType = PokeRoutineType.Idle;
    /// <inheritdoc/>
    public override void Resume() => NextRoutineType = InitialRoutine;
}
