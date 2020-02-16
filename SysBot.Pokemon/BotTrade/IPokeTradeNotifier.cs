using System;
using PKHeX.Core;

namespace SysBot.Pokemon
{
    public interface IPokeTradeNotifier<T> where T : PKM, new()
    {
        void TradeInitialize(PokeRoutineExecutor routine, PokeTradeDetail<T> info);
        void TradeSearching(PokeRoutineExecutor routine, PokeTradeDetail<T> info);
        void TradeCanceled(PokeRoutineExecutor routine, PokeTradeDetail<T> info, PokeTradeResult msg);
        void TradeFinished(PokeRoutineExecutor routine, PokeTradeDetail<T> info, T result);
        void SendNotification(PokeRoutineExecutor routine, PokeTradeDetail<T> info, string message);
        Action? OnFinish { set; }
    }
}