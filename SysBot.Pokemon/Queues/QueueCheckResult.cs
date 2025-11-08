using PKHeX.Core;

namespace SysBot.Pokemon;

/// <summary>
/// Stores data for indicating how a queue position/presence check resulted.
/// </summary>
/// <typeparam name="T"></typeparam>
public sealed record QueueCheckResult<T>(
    bool InQueue = false,
    TradeEntry<T>? Detail = null,
    int Position = -1,
    int QueueCount = -1)
    where T : PKM, new()
{
    public static readonly QueueCheckResult<T> None = new();

    public string GetMessage()
    {
        if (!InQueue || Detail is null)
            return "You are not in the queue.";
        var position = $"{Position}/{QueueCount}";
        var msg = $"You are in the {Detail.Type} queue! Position: {position} (ID {Detail.Trade.ID})";
        var pk = Detail.Trade.TradeData;
        if (pk.Species != 0)
            msg += $", Receiving: {GameInfo.GetStrings("en").Species[pk.Species]}";
        return msg;
    }
}
