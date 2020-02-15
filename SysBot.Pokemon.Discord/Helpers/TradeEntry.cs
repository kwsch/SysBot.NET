using PKHeX.Core;

namespace SysBot.Pokemon.Discord
{
    public sealed class TradeEntry<T> where T : PKM
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
    }
}