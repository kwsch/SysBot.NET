using PKHeX.Core;
using SysBot.Base;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;

namespace SysBot.Pokemon;

public class TradeSettings : IBotStateSettings, ICountSettings
{
    private const string CountStats = nameof(CountStats);
    private const string HOMELegality = nameof(HOMELegality);
    private const string TradeConfig = nameof(TradeConfig);
    private const string VGCPastesConfig = nameof(VGCPastesConfig);
    private const string Miscellaneous = nameof(Miscellaneous);
    private const string RequestFolders = nameof(RequestFolders);

    [Category(TradeConfig), Description("Settings related to Trade Configuration."), Browsable(true)]
    public TradeSettingsCategory TradeConfiguration { get; set; } = new();

    [Category(VGCPastesConfig), Description("Settings related to VGCPastes Configuration."), Browsable(true)]
    public VGCPastesCategory VGCPastesConfiguration { get; set; } = new();

    [Category(HOMELegality), Description("Settings related to HOME Legality."), Browsable(true)]
    public HOMELegalitySettingsCategory HomeLegalitySettings { get; set; } = new();

    [Category(RequestFolders), Description("Settings related to Request Folders."), Browsable(true)]
    public RequestFolderSettingsCategory RequestFolderSettings { get; set; } = new();

    [Category(CountStats), Description("Settings related to Trade Count Statistics."), Browsable(true)]
    public CountStatsSettingsCategory CountStatsSettings { get; set; } = new();


    [Category(TradeConfig), TypeConverter(typeof(CategoryConverter<TradeSettingsCategory>))]
    public class TradeSettingsCategory
    {
        public override string ToString() => "Trade Configuration Settings";

        [Category(TradeConfig), Description("Minimum Link Code.")]
        public int MinTradeCode { get; set; } = 0;

        [Category(TradeConfig), Description("Maximum Link Code.")]
        public int MaxTradeCode { get; set; } = 9999_9999;

        [Category(TradeConfig), Description("Time to wait for a trade partner in seconds.")]
        public int TradeWaitTime { get; set; } = 30;

        [Category(TradeConfig), Description("Max amount of time in seconds pressing A to wait for a trade to process.")]
        public int MaxTradeConfirmTime { get; set; } = 25;

        [Category(TradeConfig), Description("Select default species for \"ItemTrade\", if configured.")]
        public Species ItemTradeSpecies { get; set; } = Species.None;

        [Category(TradeConfig), Description("Default held item to send if none is specified.")]
        public HeldItem DefaultHeldItem { get; set; } = HeldItem.None;

        [Category(TradeConfig), Description("If set to True, each valid Pokemon will come with all suggested Relearnable Moves without the need for a batch command.")]
        public bool SuggestRelearnMoves { get; set; } = true;

        [Category(TradeConfig), Description("Toggle to allow or disallow batch trades.")]
        public bool AllowBatchTrades { get; set; } = true;

        [Category(TradeConfig), Description("Maximum pokemons of single trade. Batch mode will be closed if this configuration is less than 1")]
        public int MaxPkmsPerTrade { get; set; } = 1;

        [Category(TradeConfig), Description("Dump Trade: Dumping routine will stop after a maximum number of dumps from a single user.")]
        public int MaxDumpsPerTrade { get; set; } = 20;

        [Category(TradeConfig), Description("Dump Trade: Dumping routine will stop after spending x seconds in trade.")]
        public int MaxDumpTradeTime { get; set; } = 180;

        [Category(TradeConfig), Description("Dump Trade: If enabled, Dumping routine will output legality check information to the user.")]
        public bool DumpTradeLegalityCheck { get; set; } = true;

        [Category(TradeConfig), Description("LGPE Setting.")]
        public int TradeAnimationMaxDelaySeconds = 25;

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
    }

    [Category(VGCPastesConfig), TypeConverter(typeof(CategoryConverter<VGCPastesCategory>))]
    public class VGCPastesCategory
    {
        public override string ToString() => "VGCPastes Configuration Settings";

        [Category(VGCPastesConfig), Description("Allow users to request and generate teams using the VGCPastes Spreadsheet.")]
        public bool AllowRequests { get; set; } = true;

        [Category(VGCPastesConfig), Description("GID of Spreadsheet tab you would like to pull from.  Hint: https://docs.google.com/spreadsheets/d/ID/gid=1837599752")]
        public int GID { get; set; } = 1837599752; // Reg F Tab

    }


    [Category(HOMELegality), TypeConverter(typeof(CategoryConverter<HOMELegalitySettingsCategory>))]
    public class HOMELegalitySettingsCategory
    {
        public override string ToString() => "HOME Legality Settings";

        [Category(HOMELegality), Description("Prevents trading Pokémon that require a HOME Tracker, even if the file has one already.")]
        public bool DisallowNonNatives { get; set; } = false;

        [Category(HOMELegality), Description("Prevents trading Pokémon that already have a HOME Tracker.")]
        public bool DisallowTracked { get; set; } = false;
    }

    [Category(RequestFolders), TypeConverter(typeof(CategoryConverter<RequestFolderSettingsCategory>))]
    public class RequestFolderSettingsCategory
    {
        public override string ToString() => "Request Folders Settings";

        [Category("RequestFolders"), Description("Path to your Events Folder. Create a new folder called 'events' and copy the path here.")]
        public string EventsFolder { get; set; } = string.Empty;

        [Category("RequestFolders"), Description("Path to your BattleReady Folder. Create a new folder called 'battleready' and copy the path here.")]
        public string BattleReadyPKMFolder { get; set; } = string.Empty;
    }

    [Category(Miscellaneous), Description("Miscellaneous Settings")]
    public bool ScreenOff { get; set; } = false;

    /// <summary>
    /// Gets a random trade code based on the range settings.
    /// </summary>
    public int GetRandomTradeCode() => Util.Rand.Next(TradeConfiguration.MinTradeCode, TradeConfiguration.MaxTradeCode + 1);
    public List<Pictocodes> GetRandomLGTradeCode(bool randomtrade = false)
    {
        var lgcode = new List<Pictocodes>();
        if (randomtrade)
        {
            for (int i = 0; i <= 2; i++)
            {
                // code.Add((pictocodes)Util.Rand.Next(10));
                lgcode.Add(Pictocodes.Pikachu);

            }
        }
        else
        {
            for (int i = 0; i <= 2; i++)
            {
                lgcode.Add((Pictocodes)Util.Rand.Next(10));
                // code.Add(pictocodes.Pikachu);

            }
        }
        return lgcode;
    }


    [Category(CountStats), TypeConverter(typeof(CategoryConverter<CountStatsSettingsCategory>))]
    public class CountStatsSettingsCategory
    {
        public override string ToString() => "Trade Count Statistics";

        private int _completedSurprise;
        private int _completedDistribution;
        private int _completedTrades;
        private int _completedSeedChecks;
        private int _completedClones;
        private int _completedDumps;
        private int _completedFixOTs;

        [Category(CountStats), Description("Completed Surprise Trades")]
        public int CompletedSurprise
        {
            get => _completedSurprise;
            set => _completedSurprise = value;
        }

        [Category(  ), Description("Completed Link Trades (Distribution)")]
        public int CompletedDistribution
        {
            get => _completedDistribution;
            set => _completedDistribution = value;
        }

        [Category(CountStats), Description("Completed Link Trades (Specific User)")]
        public int CompletedTrades
        {
            get => _completedTrades;
            set => _completedTrades = value;
        }

        [Category(CountStats), Description("Completed FixOT Trades (Specific User)")]
        public int CompletedFixOTs
        {
            get => _completedFixOTs;
            set => _completedFixOTs = value;
        }

        [Browsable(false)]
        [Category(CountStats), Description("Completed Seed Check Trades")]
        public int CompletedSeedChecks
        {
            get => _completedSeedChecks;
            set => _completedSeedChecks = value;
        }

        [Category(CountStats), Description("Completed Clone Trades (Specific User)")]
        public int CompletedClones
        {
            get => _completedClones;
            set => _completedClones = value;
        }

        [Category(CountStats), Description("Completed Dump Trades (Specific User)")]
        public int CompletedDumps
        {
            get => _completedDumps;
            set => _completedDumps = value;
        }

        [Category(CountStats), Description("When enabled, the counts will be emitted when a status check is requested.")]
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

    public bool EmitCountsOnStatusCheck
    {
        get => CountStatsSettings.EmitCountsOnStatusCheck;
        set => CountStatsSettings.EmitCountsOnStatusCheck = value;
    }

    public IEnumerable<string> GetNonZeroCounts()
    {
        // Delegating the call to CountStatsSettingsCategory
        return CountStatsSettings.GetNonZeroCounts();
    }

    public class CategoryConverter<T> : TypeConverter
    {
        public override bool GetPropertiesSupported(ITypeDescriptorContext? context) => true;

        public override PropertyDescriptorCollection GetProperties(ITypeDescriptorContext? context, object value, Attribute[]? attributes) => TypeDescriptor.GetProperties(typeof(T));

        public override bool CanConvertTo(ITypeDescriptorContext? context, Type? destinationType) => destinationType != typeof(string) && base.CanConvertTo(context, destinationType);
    }
}
