using PKHeX.Core;
using SysBot.Base;
using System;
using System.Linq;

namespace SysBot.Pokemon;

public class PokeTradeLogNotifier<T> : IPokeTradeNotifier<T> where T : PKM, new()
{
    public void TradeInitialize(PokeRoutineExecutor<T> routine, PokeTradeDetail<T> info)
    {
        LogUtil.LogInfo($"Beginn der Handelsschleife für {info.Trainer.TrainerName}, sende {GameInfo.GetStrings(1).Species[info.TradeData.Species]}", routine.Connection.Label);
    }

    public void TradeSearching(PokeRoutineExecutor<T> routine, PokeTradeDetail<T> info)
    {
        LogUtil.LogInfo($"Auf der Suche nach Handel mit {info.Trainer.TrainerName}, sende {GameInfo.GetStrings(1).Species[info.TradeData.Species]}", routine.Connection.Label);
    }

    public void TradeCanceled(PokeRoutineExecutor<T> routine, PokeTradeDetail<T> info, PokeTradeResult msg)
    {
        LogUtil.LogInfo($"Abbruch des Handels mit {info.Trainer.TrainerName}, weil {msg}.", routine.Connection.Label);
        OnFinish?.Invoke(routine);
    }

    public void TradeFinished(PokeRoutineExecutor<T> routine, PokeTradeDetail<T> info, T result)
    {
        LogUtil.LogInfo($"Beendeter Handel {info.Trainer.TrainerName} {GameInfo.GetStrings(1).Species[info.TradeData.Species]} für {GameInfo.GetStrings(1).Species[result.Species]}", routine.Connection.Label);
        OnFinish?.Invoke(routine);
    }

    public void SendNotification(PokeRoutineExecutor<T> routine, PokeTradeDetail<T> info, string message)
    {
        LogUtil.LogInfo(message, routine.Connection.Label);
    }

    public void SendNotification(PokeRoutineExecutor<T> routine, PokeTradeDetail<T> info, PokeTradeSummary message)
    {
        var msg = message.Summary;
        if (message.Details.Count > 0)
            msg += ", " + string.Join(", ", message.Details.Select(z => $"{z.Heading}: {z.Detail}"));
        LogUtil.LogInfo(msg, routine.Connection.Label);
    }

    public void SendNotification(PokeRoutineExecutor<T> routine, PokeTradeDetail<T> info, T result, string message)
    {
        LogUtil.LogInfo($"Benachrichtige {info.Trainer.TrainerName} über ihres {GameInfo.GetStrings(1).Species[result.Species]}", routine.Connection.Label);
        LogUtil.LogInfo(message, routine.Connection.Label);
    }

    public Action<PokeRoutineExecutor<T>>? OnFinish { get; set; }
}
