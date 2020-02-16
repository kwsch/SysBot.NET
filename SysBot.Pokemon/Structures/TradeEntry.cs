using PKHeX.Core;

namespace SysBot.Pokemon
{
    public sealed class TradeEntry<T> where T : PKM, new()
    {
        public readonly ulong User;
        public readonly string Name;
        public readonly PokeTradeDetail<T> Trade;
        public readonly PokeRoutineType Type;

        public TradeEntry(PokeTradeDetail<T> trade, ulong user, PokeRoutineType type, string name)
        {
            Trade = trade;
            User = user;
            Type = type;
            Name = name;
        }

        public bool Equals(ulong uid, PokeRoutineType type = 0)
        {
            if (User != uid)
                return false;
            return type == 0 || type == Type;
        }
    }
}