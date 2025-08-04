using PKHeX.Core;
using System;
using System.Collections.Generic;
using System.Threading;

namespace SysBot.Pokemon
{
    public class PokeTradeDetail<TPoke> : IEquatable<PokeTradeDetail<TPoke>>, IFavoredEntry where TPoke : PKM, new()
    {
        // ReSharper disable once StaticMemberInGenericType
        /// <summary> Global variable indicating the amount of trades created. </summary>
        private static int CreatedCount;

        /// <summary> Indicates if this trade data should be given priority for queue insertion. </summary>
        public bool IsFavored { get; }

        /// <summary> Customized trade parameters. </summary>
        public Dictionary<string, object> Context = [];

        /// <summary>
        /// Trade Code
        /// </summary>
        public readonly int Code;

        /// <summary> Data to be traded </summary>
        public TPoke TradeData;

        /// <summary> Trainer details </summary>
        public readonly PokeTradeTrainerInfo Trainer;

        /// <summary> Destination to be notified for status updates </summary>
        public readonly IPokeTradeNotifier<TPoke> Notifier;

        /// <summary> Type of trade this object is for </summary>
        public readonly PokeTradeType Type;

        /// <summary> Time the object was created at </summary>
        public readonly DateTime Time;

        /// <summary> Unique incremented ID </summary>
        public readonly int ID;

        /// <summary> Indicates if the trade data should be synchronized with other bots. </summary>
        public bool IsSynchronized => Type == PokeTradeType.Random;

        /// <summary> Indicates if the trade failed at least once and is being tried again. </summary>
        public bool IsRetry;

        /// <summary> Indicates if the trade data is currently being traded. </summary>
        public bool IsProcessing;

        public List<Pictocodes> LGPETradeCode;

        public readonly int BatchTradeNumber;

        public readonly int TotalBatchTrades;

        public readonly int UniqueTradeID;

        public string? MetDate { get; set; }

        public bool IsCanceled { get; set; }

        public bool IsMysteryEgg { get; }

        public bool IgnoreAutoOT { get; }

        public bool SetEdited { get; set; }
        public List<TPoke>? BatchTrades { get; set; }

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

        public PokeTradeDetail(TPoke pkm, PokeTradeTrainerInfo info, IPokeTradeNotifier<TPoke> notifier, PokeTradeType type, int code, bool favored = false, List<Pictocodes>? lgcode = null, int batchTradeNumber = 0, int totalBatchTrades = 0, bool isMysteryEgg = false, int uniqueTradeID = 0, bool ignoreAutoOT = false, bool setEdited = false)
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        {
            ID = Interlocked.Increment(ref CreatedCount) % 50000;
            Code = code;
            TradeData = pkm;
            Trainer = info;
            Notifier = notifier;
            Type = type;
            Time = DateTime.Now;
            IsFavored = favored;
#pragma warning disable CS8601 // Possible null reference assignment.
            LGPETradeCode = lgcode;
#pragma warning restore CS8601 // Possible null reference assignment.
            BatchTradeNumber = batchTradeNumber;
            TotalBatchTrades = totalBatchTrades;
            IsMysteryEgg = isMysteryEgg;
            UniqueTradeID = uniqueTradeID;
            IgnoreAutoOT = ignoreAutoOT;
            SetEdited = setEdited;
        }

        public void TradeInitialize(PokeRoutineExecutor<TPoke> routine) => Notifier.TradeInitialize(routine, this);

        public void TradeSearching(PokeRoutineExecutor<TPoke> routine) => Notifier.TradeSearching(routine, this);

        public void TradeCanceled(PokeRoutineExecutor<TPoke> routine, PokeTradeResult msg) => Notifier.TradeCanceled(routine, this, msg);

        public virtual void TradeFinished(PokeRoutineExecutor<TPoke> routine, TPoke result)
        {
            Notifier.TradeFinished(routine, this, result);
        }

        public void SendNotification(PokeRoutineExecutor<TPoke> routine, string message) => Notifier.SendNotification(routine, this, message);

        public void SendNotification(PokeRoutineExecutor<TPoke> routine, PokeTradeSummary obj) => Notifier.SendNotification(routine, this, obj);

        public void SendNotification(PokeRoutineExecutor<TPoke> routine, TPoke obj, string message) => Notifier.SendNotification(routine, this, obj, message);

        public bool Equals(PokeTradeDetail<TPoke>? other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            return Trainer.ID == other.Trainer.ID && UniqueTradeID == other.UniqueTradeID;
        }

        public override bool Equals(object? obj)
        {
            if (obj is null) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((PokeTradeDetail<TPoke>)obj);
        }

        public override int GetHashCode() => HashCode.Combine(Trainer.ID, UniqueTradeID);

        public override string ToString() => $"{Trainer.TrainerName} - {Code}";

        public string Summary(int queuePosition)
        {
            if (TradeData.Species == 0)
                return $"{queuePosition:00}: {Trainer.TrainerName}";
            return $"{queuePosition:00}: {Trainer.TrainerName}, {(Species)TradeData.Species}";
        }
    }

    public enum Pictocodes
    {
        Pikachu,

        Eevee,

        Bulbasaur,

        Charmander,

        Squirtle,

        Pidgey,

        Caterpie,

        Rattata,

        Jigglypuff,

        Diglett
    }
}
