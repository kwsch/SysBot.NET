using System;
using System.Threading;
using System.Threading.Tasks;
using PKHeX.Core;

namespace SysBot.Pokemon;

public sealed record PokeTradeDetail<TPoke> : IFavoredEntry, IReadyStatus where TPoke : PKM, new()
{
    // ReSharper disable once StaticMemberInGenericType
    /// <summary> Global variable indicating the amount of trades created. </summary>
    private static int _createdCount;

    /// <summary> Indicates if this trade data should be given priority for queue insertion. </summary>
    public bool IsFavored { get; init; }

    /// <summary>
    /// Trade Code
    /// </summary>
    public required int Code { get; init; }

    /// <summary> Data to be traded </summary>
    public required TPoke TradeData { get; set; }

    /// <summary> Trainer details </summary>
    public required PokeTradeTrainerInfo Trainer { get; init; }

    /// <summary> Destination to be notified for status updates </summary>
    public required IPokeTradeNotifier<TPoke> Notifier { get; init; }

    /// <summary> Type of trade this object is for </summary>
    public required PokeTradeType Type { get; init; }

    /// <summary> Time the object was created at </summary>
    public DateTime Time { get; } = DateTime.UtcNow;

    /// <summary> Indicates how old the request is. </summary>
    public TimeSpan Age => DateTime.UtcNow - Time;

    /// <summary> Internal readiness state to prevent a bot from picking up the trade too early in the event it shouldn't have been queued. </summary>
    public bool IsReady { get; set; }

    /// <summary> Unique incremented ID </summary>
    public readonly int Id = Interlocked.Increment(ref _createdCount) % 3000;

    /// <summary> Indicates if the trade data should be synchronized with other bots. </summary>
    public bool IsSynchronized => Type == PokeTradeType.Random;

    /// <summary> Indicates if the trade failed at least once and is being tried again. </summary>
    public bool IsRetry { get; set; }

    /// <summary> Indicates if the trade data is currently being traded. </summary>
    public bool IsProcessing { get; set; }

    public async Task TradeInitialize(PokeRoutineExecutor<TPoke> routine) => await Notifier.TradeInitialize(routine, this).ConfigureAwait(false);
    public async Task TradeSearching(PokeRoutineExecutor<TPoke> routine) => await Notifier.TradeSearching(routine, this).ConfigureAwait(false);
    public async Task TradeCanceled(PokeRoutineExecutor<TPoke> routine, PokeTradeResult msg) => await Notifier.TradeCanceled(routine, this, msg).ConfigureAwait(false);
    public async Task TradeFinished(PokeRoutineExecutor<TPoke> routine, TPoke result) => await Notifier.TradeFinished(routine, this, result).ConfigureAwait(false);
    public async Task SendNotification(PokeRoutineExecutor<TPoke> routine, string message) => await Notifier.SendNotification(routine, this, message).ConfigureAwait(false);
    public async Task SendNotification(PokeRoutineExecutor<TPoke> routine, PokeTradeSummary obj) => await Notifier.SendNotification(routine, this, obj).ConfigureAwait(false);
    public async Task SendNotification(PokeRoutineExecutor<TPoke> routine, TPoke obj, string message) => await Notifier.SendNotification(routine, this, obj, message).ConfigureAwait(false);

    public override string ToString() => $"{Trainer.TrainerName} - {Code}";

    public string Summary(int queuePosition)
    {
        if (TradeData.Species == 0)
            return $"{queuePosition:00}: {Trainer.TrainerName}";
        return $"{queuePosition:00}: {Trainer.TrainerName}, {(Species)TradeData.Species}";
    }
}
