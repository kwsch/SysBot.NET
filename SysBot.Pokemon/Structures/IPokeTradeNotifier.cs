using PKHeX.Core;

namespace SysBot.Pokemon
{
    public interface IPokeTradeNotifier<T> where T : PKM
    {
        void TradeInitialize(PokeRoutineExecutor routine, PokeTradeDetail<T> info);
        void TradeSearching(PokeRoutineExecutor routine, PokeTradeDetail<T> info);
        void TradeFinished(PokeRoutineExecutor routine, PokeTradeDetail<T> info);
    }
}