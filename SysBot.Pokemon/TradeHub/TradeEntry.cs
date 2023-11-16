using PKHeX.Core;

namespace SysBot.Pokemon;

/// <summary>
/// Contains the details about an out-of-game player's data request to be traded in-game.
/// </summary>
/// <typeparam name="T">Format specific to the game it is received in</typeparam>
public sealed record TradeEntry<T> where T : PKM, new()
{
    public readonly ulong UserID;
    public readonly string Username;
    public readonly PokeTradeDetail<T> Trade;
    public readonly PokeRoutineType Type;

    public TradeEntry(PokeTradeDetail<T> trade, ulong userID, PokeRoutineType type, string username)
    {
        Trade = trade;
        UserID = userID;
        Type = type;
        Username = username;
    }

    /// <summary>
    /// Checks if the provided <see cref="uid"/> matches this object's data.
    /// </summary>
    /// <param name="uid"></param>
    /// <param name="type"></param>
    /// <returns></returns>
    public bool Equals(ulong uid, PokeRoutineType type = 0)
    {
        if (UserID != uid)
            return false;
        return type == 0 || type == Type;
    }

    public override string ToString() => $"(ID {Trade.ID}) {Username} {UserID:D19} - {Type}";
}
