using System;
using System.Linq;
using System.Threading.Tasks;
using PKHeX.Core;
using SysBot.Base;

namespace SysBot.Pokemon;

public class PokeTradeLogNotifier<T> : IPokeTradeNotifier<T> where T : PKM, new()
{
    public Task TradeInitialize(PokeRoutineExecutor<T> routine, PokeTradeDetail<T> info)
    {
        LogUtil.LogInfo($"Starting trade loop for {info.Trainer.TrainerName}, sending {routine.GetSpeciesName(info.TradeData.Species)}", routine.Connection.Label);
        return Task.CompletedTask;
    }

    public Task TradeSearching(PokeRoutineExecutor<T> routine, PokeTradeDetail<T> info)
    {
        LogUtil.LogInfo($"Searching for trade with {info.Trainer.TrainerName}, sending {routine.GetSpeciesName(info.TradeData.Species)}", routine.Connection.Label);
        return Task.CompletedTask;
    }

    public Task TradeCanceled(PokeRoutineExecutor<T> routine, PokeTradeDetail<T> info, PokeTradeResult msg)
    {
        LogUtil.LogInfo($"Canceling trade with {info.Trainer.TrainerName}, because {msg}.", routine.Connection.Label);
        OnFinish?.Invoke(routine);
        return Task.CompletedTask;
    }

    public Task TradeFinished(PokeRoutineExecutor<T> routine, PokeTradeDetail<T> info, T result)
    {
        // Print the nickname for Ledy trades so we can see what was requested.
        var ledyname = string.Empty;
        if (info.Trainer.TrainerName == "Random Distribution" && result.IsNicknamed)
            ledyname = $" (Nickname: \"{result.Nickname}\")";

        LogUtil.LogInfo($"Finished trading {info.Trainer.TrainerName} {routine.GetSpeciesName(info.TradeData.Species)} for {routine.GetSpeciesName(result.Species)}{ledyname}", routine.Connection.Label);
        OnFinish?.Invoke(routine);
        return Task.CompletedTask;
    }

    public Task SendNotification(PokeRoutineExecutor<T> routine, PokeTradeDetail<T> info, string message)
    {
        LogUtil.LogInfo(message, routine.Connection.Label);
        return Task.CompletedTask;
    }

    public Task SendNotification(PokeRoutineExecutor<T> routine, PokeTradeDetail<T> info, PokeTradeSummary trade)
    {
        var msg = trade.Summary;
        if (trade.Details.Count > 0)
            msg += ", " + string.Join(", ", trade.Details.Select(z => $"{z.Heading}: {z.Detail}"));
        LogUtil.LogInfo(msg, routine.Connection.Label);
        return Task.CompletedTask;
    }

    public Task SendNotification(PokeRoutineExecutor<T> routine, PokeTradeDetail<T> info, T result, string message)
    {
        LogUtil.LogInfo($"Notifying {info.Trainer.TrainerName} about their {routine.GetSpeciesName(result.Species)}", routine.Connection.Label);
        LogUtil.LogInfo(message, routine.Connection.Label);
        return Task.CompletedTask;
    }

    public Action<PokeRoutineExecutor<T>>? OnFinish { get; set; }
}
