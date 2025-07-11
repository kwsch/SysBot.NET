using PKHeX.Core;
using System;

namespace SysBot.Pokemon;

public interface IPokeTradeNotifier<T> where T : PKM, new()
{
    /// <summary> Notifies when a trade bot is initializing at the start. </summary>
    void TradeInitialize(PokeRoutineExecutor<T> routine, PokeTradeDetail<T> info);
    /// <summary> Notifies when a trade bot is searching for the partner. </summary>
    void TradeSearching(PokeRoutineExecutor<T> routine, PokeTradeDetail<T> info);
    /// <summary> Notifies when a trade bot notices the trade was canceled. </summary>
    void TradeCanceled(PokeRoutineExecutor<T> routine, PokeTradeDetail<T> info, PokeTradeResult msg);
    /// <summary> Notifies when a trade bot finishes the trade. </summary>
    void TradeFinished(PokeRoutineExecutor<T> routine, PokeTradeDetail<T> info, T result);

    /// <summary> Sends a notification when called with parameters. </summary>
    void SendNotification(PokeRoutineExecutor<T> routine, PokeTradeDetail<T> info, string message);
    /// <summary> Sends a notification when called with parameters. </summary>
    void SendNotification(PokeRoutineExecutor<T> routine, PokeTradeDetail<T> info, PokeTradeSummary message);
    /// <summary> Sends a notification when called with parameters. </summary>
    void SendNotification(PokeRoutineExecutor<T> routine, PokeTradeDetail<T> info, T result, string message);

    /// <summary> Notifies when a trade bot is initializing at the start. </summary>
    Action<PokeRoutineExecutor<T>>? OnFinish { set; }
}
