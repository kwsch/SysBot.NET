﻿using PKHeX.Core;
using System;
using System.Threading;

namespace SysBot.Pokemon
{
    public class PokeTradeDetail<TPoke> : IEquatable<PokeTradeDetail<TPoke>> where TPoke : PKM, new()
    {
        // ReSharper disable once StaticMemberInGenericType
        private static int CreatedCount;

        public readonly int Code;
        public TPoke TradeData;
        public readonly PokeTradeTrainerInfo Trainer;
        public readonly IPokeTradeNotifier<TPoke> Notifier;
        public readonly PokeTradeType Type;
        public readonly DateTime Time;
        public readonly int ID; // unique incremented ID

        public bool IsSynchronized => Type == PokeTradeType.Random;
        public bool IsRetry;

        public PokeTradeDetail(TPoke pkm, PokeTradeTrainerInfo info, IPokeTradeNotifier<TPoke> notifier, PokeTradeType type, int code)
        {
            Code = code;
            TradeData = pkm;
            Trainer = info;
            Notifier = notifier;
            Type = type;
            Time = DateTime.Now;

            ID = Interlocked.Increment(ref CreatedCount) % 3000;
        }

        public void TradeInitialize(PokeRoutineExecutor routine) => Notifier.TradeInitialize(routine, this);
        public void TradeSearching(PokeRoutineExecutor routine) => Notifier.TradeSearching(routine, this);
        public void TradeCanceled(PokeRoutineExecutor routine, PokeTradeResult msg) => Notifier.TradeCanceled(routine, this, msg);

        public virtual void TradeFinished(PokeRoutineExecutor routine, TPoke result)
        {
            Notifier.TradeFinished(routine, this, result);
        }

        public void SendNotification(PokeRoutineExecutor routine, string message) => Notifier.SendNotification(routine, this, message);
        public void SendNotification(PokeRoutineExecutor routine, PokeTradeSummary obj) => Notifier.SendNotification(routine, this, obj);
        public void SendNotification(PokeRoutineExecutor routine, TPoke obj, string message) => Notifier.SendNotification(routine, this, obj, message);

        public bool Equals(PokeTradeDetail<TPoke>? other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            return ReferenceEquals(Trainer, other.Trainer);
        }

        public override bool Equals(object? obj)
        {
            if (obj is null) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((PokeTradeDetail<TPoke>)obj);
        }

        public override int GetHashCode() => Trainer.GetHashCode();
        public override string ToString() => $"{Trainer.TrainerName} - {Code}";

        public string Summary(int i)
        {
            if (TradeData.Species == 0)
                return $"{i:00}: {Trainer.TrainerName}";
            return $"{i:00}: {Trainer.TrainerName}, {(Species)TradeData.Species}";
        }
    }
}