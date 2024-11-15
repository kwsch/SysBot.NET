using PKHeX.Core;

namespace SysBot.Pokemon;

/// <summary>
/// Contains the details about an out-of-game player's data request to be traded in-game.
/// </summary>
/// <typeparam name="T">Format specific to the game it is received in</typeparam>
public sealed record TradeEntry<T>(PokeTradeDetail<T> Trade, ulong UserID, PokeRoutineType Type, string Username)
    where T : PKM, new()
{
    /// <summary>
    /// Checks if the provided <see cref="uid"/> matches this object's data.
    /// </summary>
    public bool Equals(ulong uid, PokeRoutineType type = 0)
    {
        if (UserID != uid)
            return false;
        return type == 0 || type == Type;
    }

    public override string ToString() => $"(ID {Trade.ID}) {Username} {UserID:D19} - {Type}";
}
