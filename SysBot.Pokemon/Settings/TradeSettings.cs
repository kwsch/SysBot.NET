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
    public enum HeldItem
    {
        None = 0,
        AbilityPatch = 1606,
        RareCandy = 50,
        AbilityCapsule = 645,
        BottleCap = 795,
        expCandyL = 1127,
        expCandyXL = 1128,
        MasterBall = 1,
        Nugget = 92,
        BigPearl = 89,
        GoldBottleCap = 796,
        ppUp = 51,
        ppMax = 53,
        FreshStartMochi = 2479,
    }

    public override string ToString() => "Trade Bot Settings";

    [Category(TradeConfig), Description("Time to wait for a trade partner in seconds.")]
    public int TradeWaitTime { get; set; } = 30;

    [Category(TradeConfig), Description("Max amount of time in seconds pressing A to wait for a trade to process.")]
    public int MaxTradeConfirmTime { get; set; } = 25;

    [Category(TradeConfig), Description("Toggle to allow or disallow batch trades.")]
    public bool AllowBatchTrades { get; set; } = true;

    [Category(TradeConfig), Description("Default held item to send if none is specified.")]
    public HeldItem DefaultHeldItem { get; set; } = HeldItem.AbilityPatch;

    [Category(TradeConfig), Description("Path to your Events Folder.  Create a new folder called 'events' and copy the path here.")]
    public string EventsFolder { get; set; } = string.Empty;

    [Category(TradeConfig), Description("Path to your BattleReady Folder.  Create a new folder called 'battleready' and copy the path here.")]
    public string BattleReadyPKMFolder { get; set; } = string.Empty;

    [Category(TradeCode), Description("Maximum pokemons of single trade. Batch mode will be closed if this configuration is less than 1")]
    public int MaxPkmsPerTrade { get; set; } = 1;

    [Category(TradeCode), Description("Minimum Link Code.")]
    public int MinTradeCode { get; set; } = 0;

    [Category(TradeCode), Description("Maximum Link Code.")]
    public int MaxTradeCode { get; set; } = 9999_9999;

    [Category(Dumping), Description("Dump Trade: Dumping routine will stop after a maximum number of dumps from a single user.")]
    public int MaxDumpsPerTrade { get; set; } = 20;

    [Category(Dumping), Description("Dump Trade: Dumping routine will stop after spending x seconds in trade.")]
    public int MaxDumpTradeTime { get; set; } = 180;

    [Category(Dumping), Description("Dump Trade: If enabled, Dumping routine will output legality check information to the user.")]
    public bool DumpTradeLegalityCheck { get; set; } = true;

    [Category(TradeConfig), Description("When enabled, the screen will be turned off during normal bot loop operation to save power.")]
    public bool ScreenOff { get; set; }

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
    private int _completedFixOTs;

    [Category(Counts), Description("Completed Surprise Trades")]
    public int CompletedSurprise
    {
        get => _completedSurprise;
        set => _completedSurprise = value;
    }

    [Category(Counts), Description("Completed Link Trades (Distribution)")]
    public int CompletedDistribution
    {
        get => _completedDistribution;
        set => _completedDistribution = value;
    }

    [Category(Counts), Description("Completed Link Trades (Specific User)")]
    public int CompletedTrades
    {
        get => _completedTrades;
        set => _completedTrades = value;
    }

    [Category(Counts), Description("Completed FixOT Trades (Specific User)")]
    public int CompletedFixOTs
    {
        get => _completedFixOTs;
        set => _completedFixOTs = value;
    }

    [Browsable(false)]
    [Category(Counts), Description("Completed Seed Check Trades")]
    public int CompletedSeedChecks
    {
        get => _completedSeedChecks;
        set => _completedSeedChecks = value;
    }

    [Category(Counts), Description("Completed Clone Trades (Specific User)")]
    public int CompletedClones
    {
        get => _completedClones;
        set => _completedClones = value;
    }

    [Category(Counts), Description("Completed Dump Trades (Specific User)")]
    public int CompletedDumps
    {
        get => _completedDumps;
        set => _completedDumps = value;
    }

    [Category(Counts), Description("When enabled, the counts will be emitted when a status check is requested.")]
    public bool EmitCountsOnStatusCheck { get; set; }

    public void AddCompletedTrade() => Interlocked.Increment(ref _completedTrades);
    public void AddCompletedSeedCheck() => Interlocked.Increment(ref _completedSeedChecks);
    public void AddCompletedSurprise() => Interlocked.Increment(ref _completedSurprise);
    public void AddCompletedDistribution() => Interlocked.Increment(ref _completedDistribution);
    public void AddCompletedDumps() => Interlocked.Increment(ref _completedDumps);
    public void AddCompletedClones() => Interlocked.Increment(ref _completedClones);
    public void AddCompletedFixOTs() => Interlocked.Increment(ref _completedFixOTs);

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
            yield return $"Distribution Trades: {CompletedDistribution}";
        if (CompletedFixOTs != 0)
            yield return $"FixOT Trades: {CompletedFixOTs}";
        if (CompletedSurprise != 0)
            yield return $"Surprise Trades: {CompletedSurprise}";
    }
}
