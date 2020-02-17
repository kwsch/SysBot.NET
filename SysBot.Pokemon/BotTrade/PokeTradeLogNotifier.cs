using System;
using NLog;
using PKHeX.Core;
using SysBot.Base;

namespace SysBot.Pokemon
{
    public class PokeTradeLogNotifier<T> : IPokeTradeNotifier<T> where T : PKM, new()
    {
        public void TradeInitialize(PokeRoutineExecutor routine, PokeTradeDetail<T> info)
        {
            LogUtil.Log(LogLevel.Info, $"Starting trade loop for {info.Trainer.TrainerName}, sending {(Species)info.TradeData.Species}", routine.Connection.Name);
        }

        public void TradeSearching(PokeRoutineExecutor routine, PokeTradeDetail<T> info)
        {
            LogUtil.Log(LogLevel.Info, $"Searching for trade with {info.Trainer.TrainerName}, sending {(Species)info.TradeData.Species}", routine.Connection.Name);
        }

        public void TradeCanceled(PokeRoutineExecutor routine, PokeTradeDetail<T> info, PokeTradeResult msg)
        {
            LogUtil.Log(LogLevel.Info, $"Canceling trade with {info.Trainer.TrainerName}, because {msg}.", routine.Connection.Name);
            OnFinish?.Invoke(routine);
        }

        public void TradeFinished(PokeRoutineExecutor routine, PokeTradeDetail<T> info, T result)
        {
            LogUtil.Log(LogLevel.Info, $"Finished trade for {info.Trainer.TrainerName}, sending {(Species)info.TradeData.Species}", routine.Connection.Name);
            LogUtil.Log(LogLevel.Info, $"Received: {(Species)result.Species}.", routine.Connection.Name);
            OnFinish?.Invoke(routine);
        }

        public void SendNotification(PokeRoutineExecutor routine, PokeTradeDetail<T> info, string message)
        {
            LogUtil.Log(LogLevel.Info, message, routine.Connection.Name);
        }

        public Action<PokeRoutineExecutor>? OnFinish { get; set; }
    }
}