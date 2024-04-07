using PKHeX.Core;
using SysBot.Base;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;

namespace SysBot.Pokemon;

public class TradeSettings : IBotStateSettings, ICountSettings
{
    private const string TradeCode = nameof(TradeCode);
    private const string TradeConfig = nameof(TradeConfig);
    private const string Dumping = nameof(Dumping);
    private const string Counts = nameof(Counts);
    public override string ToString() => "Trade Bot Einstellungen";

    [Category(TradeConfig), Description("Zeit in Sekunden um auf einen Handelspartner zu warten.")]
    public int TradeWaitTime { get; set; } = 30;

    [Category(TradeConfig), Description("Maximale Zeitspanne in Sekunden, die beim Drücken von A gewartet wird, bis ein Handel abgewickelt ist.")]
    public int MaxTradeConfirmTime { get; set; } = 25;

    [Category(TradeCode), Description("Minimum Link-Code.")]
    public int MinTradeCode { get; set; } = 8180;

    [Category(TradeCode), Description("Maximum Link-Code.")]
    public int MaxTradeCode { get; set; } = 8199;

    [Category(Dumping), Description("Dump-Trade: Die Dumping-Routine wird nach einer maximalen Anzahl von Dumps eines einzelnen Benutzers beendet.")]
    public int MaxDumpsPerTrade { get; set; } = 20;

    [Category(Dumping), Description("Dump-Trade: Die Dumping-Routine wird nach x Sekunden im Handel beendet.")]
    public int MaxDumpTradeTime { get; set; } = 180;

    [Category(Dumping), Description("Dump-Trade: Wenn diese Option aktiviert ist, gibt die Dumping-Routine Informationen zur Legalitätsprüfung an den Benutzer aus.")]
    public bool DumpTradeLegalityCheck { get; set; } = true;

    [Category(TradeConfig), Description("Wenn diese Funktion aktiviert ist, wird der Bildschirm während des normalen Bot-Loop-Betriebs ausgeschaltet, um Strom zu sparen.")]
    public bool ScreenOff { get; set; }

    [Category(TradeConfig), Description("Wenn diese Option aktiviert ist, wird das Anfordern von Pokémon von außerhalb ihres ursprünglichen Kontexts nicht zugelassen.")]
    public bool DisallowNonNatives { get; set; } = true;

    [Category(TradeConfig), Description("Wenn diese Option aktiviert ist, können Pokémon nicht angefordert werden, wenn sie einen HOME-Tracker haben.")]
    public bool DisallowTracked { get; set; } = true;

    /// <summary>
    /// Gets a random trade code based on the range settings.
    /// </summary>
    public int GetRandomTradeCode() => Util.Rand.Next(MinTradeCode, MaxTradeCode + 1);

    private int _completedSurprise;
    private int _completedDistribution;
    private int _completedTrades;
    private int _completedSeedChecks;
    private int _completedClones;
    private int _completedDumps;

    [Category(Counts), Description("Abgeschlossene Überraschungstrades")]
    public int CompletedSurprise
    {
        get => _completedSurprise;
        set => _completedSurprise = value;
    }

    [Category(Counts), Description("Abgeschlossene Link-Trades (Verteilung)")]
    public int CompletedDistribution
    {
        get => _completedDistribution;
        set => _completedDistribution = value;
    }

    [Category(Counts), Description("Abgeschlossene Link-Trades (bestimmter Benutzer)")]
    public int CompletedTrades
    {
        get => _completedTrades;
        set => _completedTrades = value;
    }

    [Category(Counts), Description("Abgeschlossene Seed-Check-Transaktionen")]
    public int CompletedSeedChecks
    {
        get => _completedSeedChecks;
        set => _completedSeedChecks = value;
    }

    [Category(Counts), Description("Abgeschlossene Clone Trades (Spezifischer Benutzer)")]
    public int CompletedClones
    {
        get => _completedClones;
        set => _completedClones = value;
    }

    [Category(Counts), Description("Abgeschlossene Dump Trades (spezifischer Benutzer)")]
    public int CompletedDumps
    {
        get => _completedDumps;
        set => _completedDumps = value;
    }

    [Category(Counts), Description("Wenn diese Option aktiviert ist, werden die Zählungen bei der Anforderung einer Statusprüfung ausgegeben.")]
    public bool EmitCountsOnStatusCheck { get; set; }

    public void AddCompletedTrade() => Interlocked.Increment(ref _completedTrades);
    public void AddCompletedSeedCheck() => Interlocked.Increment(ref _completedSeedChecks);
    public void AddCompletedSurprise() => Interlocked.Increment(ref _completedSurprise);
    public void AddCompletedDistribution() => Interlocked.Increment(ref _completedDistribution);
    public void AddCompletedDumps() => Interlocked.Increment(ref _completedDumps);
    public void AddCompletedClones() => Interlocked.Increment(ref _completedClones);

    public IEnumerable<string> GetNonZeroCounts()
    {
        if (!EmitCountsOnStatusCheck)
            yield break;
        if (CompletedSeedChecks != 0)
            yield return $"Seed Check Trades: {CompletedSeedChecks}";
        if (CompletedClones != 0)
            yield return $"Clone Trades: {CompletedClones}";
        if (CompletedDumps != 0)
            yield return $"Dump Trades: {CompletedDumps}";
        if (CompletedTrades != 0)
            yield return $"Link Trades: {CompletedTrades}";
        if (CompletedDistribution != 0)
            yield return $"Verteilungs-Trades: {CompletedDistribution}";
        if (CompletedSurprise != 0)
            yield return $"Überraschungs-Trades: {CompletedSurprise}";
    }
}
