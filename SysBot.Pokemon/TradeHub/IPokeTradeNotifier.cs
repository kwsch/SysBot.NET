using PKHeX.Core;
using System;

namespace SysBot.Pokemon
{
    public interface IPokeTradeNotifier<T> where T : PKM, new()
    {
        void TradeInitialize(PokeRoutineExecutor<T> routine, PokeTradeDetail<T> info);
        void TradeSearching(PokeRoutineExecutor<T> routine, PokeTradeDetail<T> info);
        void TradeCanceled(PokeRoutineExecutor<T> routine, PokeTradeDetail<T> info, PokeTradeResult msg);
        void TradeFinished(PokeRoutineExecutor<T> routine, PokeTradeDetail<T> info, T result);
        void SendNotification(PokeRoutineExecutor<T> routine, PokeTradeDetail<T> info, string message);
        void SendNotification(PokeRoutineExecutor<T> routine, PokeTradeDetail<T> info, PokeTradeSummary message);
        void SendNotification(PokeRoutineExecutor<T> routine, PokeTradeDetail<T> info, T result, string message);
        Action<PokeRoutineExecutor<T>>? OnFinish { set; }
    }
}