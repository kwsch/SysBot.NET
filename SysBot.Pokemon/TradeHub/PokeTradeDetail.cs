using PKHeX.Core;
using System;
using System.Threading;
using System.Collections.Generic;

namespace SysBot.Pokemon;

public class PokeTradeDetail<TPoke>(TPoke TradeData, PokeTradeTrainerInfo Trainer, IPokeTradeNotifier<TPoke> Notifier, PokeTradeType Type, int Code, bool IsFavored = false) : IEquatable<PokeTradeDetail<TPoke>>, IFavoredEntry where TPoke : PKM, new()
{
    // ReSharper disable once StaticMemberInGenericType
    /// <summary> Global variable indicating the amount of trades created. </summary>
    private static int CreatedCount;
    /// <summary> Indicates if this trade data should be given priority for queue insertion. </summary>
    public bool IsFavored { get; } = IsFavored;

    /// <summary>
    /// Trade Code
    /// </summary>
    public readonly int Code = Code;

    /// <summary> Data to be traded </summary>
    public TPoke TradeData = TradeData;

    /// <summary> Trainer details </summary>
    public readonly PokeTradeTrainerInfo Trainer = Trainer;

    /// <summary> Destination to be notified for status updates </summary>
    public readonly IPokeTradeNotifier<TPoke> Notifier = Notifier;

    /// <summary> Type of trade this object is for </summary>
    public readonly PokeTradeType Type = Type;

    /// <summary> Time the object was created at </summary>
    public readonly DateTime Time = DateTime.Now;
    /// <summary> Unique incremented ID </summary>
    public readonly int ID = Interlocked.Increment(ref CreatedCount) % 3000;

    /// <summary> Indicates if the trade data should be synchronized with other bots. </summary>
    public bool IsSynchronized => Type == PokeTradeType.Random;

    /// <summary> Indicates if the trade failed at least once and is being tried again. </summary>
    public bool IsRetry;

    /// <summary> Indicates if the trade data is currently being traded. </summary>
    public bool IsProcessing;

    /// <summary> Customized trade parameters. </summary>
    public Dictionary<string, object> Context = new();

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
        return ReferenceEquals(Trainer, other.Trainer);
    }

    public override bool Equals(object? obj)
    {
        if (obj is null) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != GetType()) return false;
        return Equals((PokeTradeDetail<TPoke>)obj);
    }

    public override int GetHashCode() => Trainer.GetHashCode();
    public override string ToString() => $"{Trainer.TrainerName} - {Code}";

    public string Summary(int queuePosition)
    {
        if (TradeData.Species == 0)
            return $"{queuePosition:00}: {Trainer.TrainerName}";
        return $"{queuePosition:00}: {Trainer.TrainerName}, {(Species)TradeData.Species}";
    }
}
