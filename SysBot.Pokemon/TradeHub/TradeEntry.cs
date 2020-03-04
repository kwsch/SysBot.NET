using PKHeX.Core;

namespace SysBot.Pokemon
{
    public sealed class TradeEntry<T> where T : PKM, new()
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

        public bool Equals(ulong uid, PokeRoutineType type = 0)
        {
            if (UserID != uid)
                return false;
            return type == 0 || type == Type;
        }

        public override string ToString() => $"(ID {Trade.ID}) {Username} {UserID:D19} - {Type}";
    }
}