using System;
using System.Threading.Tasks;
using PKHeX.Core;

namespace SysBot.Pokemon;

public interface IPokeTradeNotifier<T> where T : PKM, new()
{
    /// <summary> Notifies when a trade bot is initializing at the start. </summary>
    Task TradeInitialize(PokeRoutineExecutor<T> routine, PokeTradeDetail<T> info);

    /// <summary> Notifies when a trade bot is searching for the partner. </summary>
    Task TradeSearching(PokeRoutineExecutor<T> routine, PokeTradeDetail<T> info);

    /// <summary> Notifies when a trade bot notices the trade was canceled. </summary>
    Task TradeCanceled(PokeRoutineExecutor<T> routine, PokeTradeDetail<T> info, PokeTradeResult msg);

    /// <summary> Notifies when a trade bot finishes the trade. </summary>
    Task TradeFinished(PokeRoutineExecutor<T> routine, PokeTradeDetail<T> info, T result);

    /// <summary> Sends a notification when called with parameters. </summary>
    Task SendNotification(PokeRoutineExecutor<T> routine, PokeTradeDetail<T> info, string message);

    /// <summary> Sends a notification when called with parameters. </summary>
    Task SendNotification(PokeRoutineExecutor<T> routine, PokeTradeDetail<T> info, PokeTradeSummary trade);

    /// <summary> Sends a notification when called with parameters. </summary>
    Task SendNotification(PokeRoutineExecutor<T> routine, PokeTradeDetail<T> info, T result, string message);

    /// <summary> Notifies when a trade bot is finishing its routine. </summary>
    Action<PokeRoutineExecutor<T>>? OnFinish { set; }
}
