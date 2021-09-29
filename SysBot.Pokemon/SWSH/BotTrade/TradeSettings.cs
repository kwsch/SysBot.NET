using System.Collections.Generic;
using PKHeX.Core;
using System.ComponentModel;
using System.Threading;
using SysBot.Base;

namespace SysBot.Pokemon
{
    public class TradeSettings : IBotStateSettings, ICountSettings
    {
        private const string TradeCode = nameof(TradeCode);
        private const string TradeConfig = nameof(TradeConfig);
        private const string Dumping = nameof(Dumping);
        private const string Counts = nameof(Counts);
        private const string Monitoring = nameof(Monitoring);
        public override string ToString() => "Trade Bot Settings";

        [Category(TradeConfig), Description("Time to wait for a trade partner in seconds.")]
        public int TradeWaitTime { get; set; } = 45;

        [Category(TradeCode), Description("Minimum Link Code.")]
        public int MinTradeCode { get; set; } = 8180;

        [Category(TradeCode), Description("Maximum Link Code.")]
        public int MaxTradeCode { get; set; } = 8199;

        [Category(Dumping), Description("Link Trade: Dumping routine will stop after a maximum number of dumps from a single user.")]
        public int MaxDumpsPerTrade { get; set; } = 20;

        [Category(Dumping), Description("Link Trade: Dumping routine will stop after spending x seconds in trade.")]
        public int MaxDumpTradeTime { get; set; } = 180;

        [Category(TradeConfig), Description("When enabled, the screen will be turned off during normal bot loop operation to save power.")]
        public bool ScreenOff { get; set; } = false;

        [Category(TradeConfig), Description("Max amount of time pressing A to wait for a trade to end before trying to exit to overworld.")]
        public int TradeAnimationMaxDelaySeconds { get; set; } = 90; // 150 maybe

        [Category(Monitoring), Description("When a person appears again in less than this setting's value (minutes), a notification will be sent.")]
        public double TradeCooldown { get; set; }

        [Category(Monitoring), Description("When a person ignores a trade cooldown, the echo message will include their Nintendo Account ID.")]
        public bool EchoNintendoOnlineIDCooldown { get; set; } = true;

        [Category(Monitoring), Description("When a person appears with a different Discord/Twitch account in less than this setting's value (minutes), a notification will be sent.")]
        public double TradeAbuseExpiration { get; set; } = 120;

        [Category(Monitoring), Description("When a person using multiple Discord/Twitch accounts is detected, the echo message will include their Nintendo Account ID.")]
        public bool EchoNintendoOnlineIDMulti { get; set; } = true;

        [Category(Monitoring), Description("When a person using multiple Discord/Twitch accounts is detected, this action is taken.")]
        public TradeAbuseAction TradeAbuseAction { get; set; } = TradeAbuseAction.Quit;

        [Category(Monitoring), Description("When a person is blocked in-game for multiple accounts, their online ID is added to BannedIDs.")]
        public bool BanIDWhenBlockingUser { get; set; } = true;

        [Category(Monitoring), Description("Banned online IDs that will trigger trade exit or in-game block.")]
        public RemoteControlAccessList BannedIDs { get; set; } = new();

        [Category(Monitoring), Description("When a person is encountered with a banned ID, block them in-game before quitting the trade.")]
        public bool BlockDetectedBannedUser { get; set; } = true;

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
        public void AddCompletedSurprise() =>Interlocked.Increment(ref _completedSurprise);
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
                yield return $"Distribution Trades: {CompletedDistribution}";
            if (CompletedSurprise != 0)
                yield return $"Surprise Trades: {CompletedSurprise}";
        }
    }
}
