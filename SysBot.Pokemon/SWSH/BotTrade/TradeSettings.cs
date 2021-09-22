using PKHeX.Core;
using System.ComponentModel;
using SysBot.Base;

namespace SysBot.Pokemon
{
    public class TradeSettings : IBotStateSettings
    {
        private const string TradeCode = nameof(TradeCode);
        private const string TradeConfig = nameof(TradeConfig);
        private const string Dumping = nameof(Dumping);
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

        [Category(TradeConfig), Description("Max amount of A presses to wait for a trade to end before trying to exit to overworld.")]
        public int TradeAnimationMaxDelaySeconds { get; set; } = 90; // 150 maybe

        /// <summary>
        /// Gets a random trade code based on the range settings.
        /// </summary>
        public int GetRandomTradeCode() => Util.Rand.Next(MinTradeCode, MaxTradeCode + 1);
    }
}
