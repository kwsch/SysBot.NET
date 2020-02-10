using NLog;
using PKHeX.Core;
using SysBot.Base;

namespace SysBot.Pokemon
{
    public class PokeTradeLogNotifier<T> : IPokeTradeNotifier<T> where T : PKM
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
        }

        public void TradeFinished(PokeRoutineExecutor routine, PokeTradeDetail<T> info)
        {
            LogUtil.Log(LogLevel.Info, $"Finished trade for {info.Trainer.TrainerName}, sending {(Species)info.TradeData.Species}", routine.Connection.Name);
        }
    }
}