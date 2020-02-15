using PKHeX.Core;

namespace SysBot.Pokemon.Discord
{
    public sealed class TradeEntry<T> where T : PKM
    {
        public readonly ulong User;
        public readonly PokeTradeDetail<T> Trade;

        public TradeEntry(PokeTradeDetail<T> trade, ulong user)
        {
            Trade = trade;
            User = user;
        }
    }
}