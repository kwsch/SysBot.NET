using System;
using System.Linq;
using PKHeX.Core;
using SysBot.Base;

namespace SysBot.Pokemon
{
    public class PokeTradeLogNotifier<T> : IPokeTradeNotifier<T> where T : PKM, new()
    {
        public void TradeInitialize(PokeRoutineExecutor routine, PokeTradeDetail<T> info)
        {
            LogUtil.LogInfo($"Starting trade loop for {info.Trainer.TrainerName}, sending {(Species)info.TradeData.Species}", routine.Connection.Name);
        }

        public void TradeSearching(PokeRoutineExecutor routine, PokeTradeDetail<T> info)
        {
            LogUtil.LogInfo($"Searching for trade with {info.Trainer.TrainerName}, sending {(Species)info.TradeData.Species}", routine.Connection.Name);
        }

        public void TradeCanceled(PokeRoutineExecutor routine, PokeTradeDetail<T> info, PokeTradeResult msg)
        {
            LogUtil.LogInfo($"Canceling trade with {info.Trainer.TrainerName}, because {msg}.", routine.Connection.Name);
            OnFinish?.Invoke(routine);
        }

        public void TradeFinished(PokeRoutineExecutor routine, PokeTradeDetail<T> info, T result)
        {
            LogUtil.LogInfo($"Finished trading {info.Trainer.TrainerName} {(Species)info.TradeData.Species} for {(Species)result.Species}", routine.Connection.Name);
            OnFinish?.Invoke(routine);
        }

        public void SendNotification(PokeRoutineExecutor routine, PokeTradeDetail<T> info, string message)
        {
            LogUtil.LogInfo(message, routine.Connection.Name);
        }

        public void SendNotification(PokeRoutineExecutor routine, PokeTradeDetail<T> info, PokeTradeSummary message)
        {
            var msg = message.Summary;
            if (message.Details.Count > 0)
                msg += ", " + string.Join(", ", message.Details.Select(z => $"{z.Heading}: {z.Detail}"));
            LogUtil.LogInfo(msg, routine.Connection.Name);
        }

        public void SendNotification(PokeRoutineExecutor routine, PokeTradeDetail<T> info, T result, string message)
        {
            LogUtil.LogInfo($"Notifying {info.Trainer.TrainerName} about their {(Species)result.Species}", routine.Connection.Name);
            LogUtil.LogInfo(message, routine.Connection.Name);
        }

        public Action<PokeRoutineExecutor>? OnFinish { get; set; }
    }
}