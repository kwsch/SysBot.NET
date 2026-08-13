namespace SysBot.Pokemon;

public interface IReadyStatus
{
    /// <summary>
    /// Short-lived delay on baking a request to allow for it to be pruned shortly after adding.
    /// </summary>
    bool IsReady { get; }
}
