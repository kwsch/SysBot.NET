using System;
using System.ComponentModel;
// ReSharper disable AutoPropertyCanBeMadeGetOnly.Global

namespace SysBot.Pokemon;

public class QueueSettings
{
    private const string FeatureToggle = nameof(FeatureToggle);
    private const string UserBias = nameof(UserBias);
    private const string TimeBias = nameof(TimeBias);
    private const string QueueToggle = nameof(QueueToggle);
    public override string ToString() => "Einstellungen für Warteschlange";

    // General

    [Category(FeatureToggle), Description("Schaltet um, ob Benutzer der Warteschlange beitreten können.")]
    public bool CanQueue { get; set; } = true;

    [Category(FeatureToggle), Description("Verhindert das Hinzufügen von Benutzern, wenn sich bereits so viele Benutzer in der Warteschlange befinden.")]
    public int MaxQueueCount { get; set; } = 999;

    [Category(FeatureToggle), Description("Ermöglicht es Benutzern, die Warteschlange zu verlassen, während sie gehandelt werden.")]
    public bool CanDequeueIfProcessing { get; set; }

    [Category(FeatureToggle), Description("Legt fest, wie der Flex-Modus die Warteschlangen verarbeiten soll.")]
    public FlexYieldMode FlexMode { get; set; } = FlexYieldMode.Weighted;

    [Category(FeatureToggle), Description("Legt fest, wann die Warteschlange ein- und ausgeschaltet wird.")]
    public QueueOpening QueueToggleMode { get; set; } = QueueOpening.Threshold;

    // Queue Toggle

    [Category(QueueToggle), Description("Schwellenwert-Modus: Anzahl der Benutzer, bei denen die Warteschlange geöffnet wird.")]
    public int ThresholdUnlock { get; set; }

    [Category(QueueToggle), Description("Schwellenwert-Modus: Anzahl der Benutzer, bei denen die Warteschlange geschlossen wird.")]
    public int ThresholdLock { get; set; } = 30;

    [Category(QueueToggle), Description("Geplanter Modus: Sekunden, die die Warteschlange offen ist, bevor sie gesperrt wird.")]
    public int IntervalOpenFor { get; set; } = 5 * 60;

    [Category(QueueToggle), Description("Geplanter Modus: Sekunden, in denen die Warteschlange geschlossen ist, bevor sie entsperrt wird.")]
    public int IntervalCloseFor { get; set; } = 15 * 60;

    // Flex Users

    [Category(UserBias), Description("Beeinflusst die Gewichtung der Handelswarteschlange, je nachdem, wie viele Benutzer sich in der Warteschlange befinden.")]
    public int YieldMultCountTrade { get; set; } = 100;

    [Category(UserBias), Description("Beeinflusst die Gewichtung der Seed-Check-Warteschlange, je nachdem, wie viele Benutzer sich in der Warteschlange befinden.")]
    public int YieldMultCountSeedCheck { get; set; } = 100;

    [Category(UserBias), Description("Die Gewichtung der Klon-Warteschlange richtet sich danach, wie viele Benutzer sich in der Warteschlange befinden.")]
    public int YieldMultCountClone { get; set; } = 100;

    [Category(UserBias), Description("Beeinflusst die Gewichtung der Dump Warteschlange auf der Grundlage der Anzahl der Benutzer in der Warteschlange.")]
    public int YieldMultCountDump { get; set; } = 100;

    // Flex Time

    [Category(TimeBias), Description("Legt fest, ob das Gewicht zum Gesamtgewicht addiert oder multipliziert werden soll.")]
    public FlexBiasMode YieldMultWait { get; set; } = FlexBiasMode.Multiply;

    [Category(TimeBias), Description("Überprüft die Zeit, die seit dem Beitritt des Benutzers zur Warteschlange \"Handel\" verstrichen ist, und erhöht die Gewichtung der Warteschlange entsprechend.")]
    public int YieldMultWaitTrade { get; set; } = 1;

    [Category(TimeBias), Description("Überprüft die Zeit, die seit dem Beitritt des Benutzers zur Seed Check-Warteschlange verstrichen ist, und erhöht die Gewichtung der Warteschlange entsprechend.")]
    public int YieldMultWaitSeedCheck { get; set; } = 1;

    [Category(TimeBias), Description("Überprüft die Zeit, die seit dem Beitritt des Benutzers zur Seed Check-Warteschlange verstrichen ist, und erhöht die Gewichtung der Warteschlange entsprechend.")]
    public int YieldMultWaitClone { get; set; } = 1;

    [Category(TimeBias), Description("Überprüft die Zeit, die seit dem Beitritt des Benutzers zur Dump-Warteschlange verstrichen ist, und erhöht die Gewichtung der Warteschlange entsprechend.")]
    public int YieldMultWaitDump { get; set; } = 1;

    [Category(TimeBias), Description("Multipliziert die Anzahl der Benutzer in der Warteschlange, um eine Schätzung der Zeit zu erhalten, die bis zur Bearbeitung des Benutzers vergehen wird.")]
    public float EstimatedDelayFactor { get; set; } = 1.1f;

    private int GetCountBias(PokeTradeType type) => type switch
    {
        PokeTradeType.Seed => YieldMultCountSeedCheck,
        PokeTradeType.Clone => YieldMultCountClone,
        PokeTradeType.Dump => YieldMultCountDump,
        _ => YieldMultCountTrade,
    };

    private int GetTimeBias(PokeTradeType type) => type switch
    {
        PokeTradeType.Seed => YieldMultWaitSeedCheck,
        PokeTradeType.Clone => YieldMultWaitClone,
        PokeTradeType.Dump => YieldMultWaitDump,
        _ => YieldMultWaitTrade,
    };

    /// <summary>
    /// Gets the weight of a <see cref="PokeTradeType"/> based on the count of users in the queue and time users have waited.
    /// </summary>
    /// <param name="count">Count of users for <see cref="type"/></param>
    /// <param name="time">Next-to-be-processed user's time joining the queue</param>
    /// <param name="type">Queue type</param>
    /// <returns>Effective weight for the trade type.</returns>
    public long GetWeight(int count, DateTime time, PokeTradeType type)
    {
        var now = DateTime.Now;
        var seconds = (now - time).Seconds;

        var cb = GetCountBias(type) * count;
        var tb = GetTimeBias(type) * seconds;

        return YieldMultWait switch
        {
            FlexBiasMode.Multiply => cb * tb,
            _ => cb + tb,
        };
    }

    /// <summary>
    /// Estimates the amount of time (minutes) until the user will be processed.
    /// </summary>
    /// <param name="position">Position in the queue</param>
    /// <param name="botct">Amount of bots processing requests</param>
    /// <returns>Estimated time in Minutes</returns>
    public float EstimateDelay(int position, int botct) => (EstimatedDelayFactor * position) / botct;
}

public enum FlexBiasMode
{
    Add,
    Multiply,
}

public enum FlexYieldMode
{
    LessCheatyFirst,
    Weighted,
}

public enum QueueOpening
{
    Manual,
    Threshold,
    Interval,
}
