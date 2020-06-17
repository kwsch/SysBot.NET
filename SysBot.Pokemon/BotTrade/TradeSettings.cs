using System.ComponentModel;
using PKHeX.Core;

namespace SysBot.Pokemon
{
    public class TradeSettings
    {
        private const string TradeCode = nameof(TradeCode);
        private const string Dumping = nameof(Dumping);
        private const string Cosmetic = nameof(Cosmetic);
        public override string ToString() => "Trade Bot Settings";

        [Category(TradeCode), Description("Minimum Link Code.")]
        public int MinTradeCode { get; set; } = 8180;

        [Category(TradeCode), Description("Maximum Link Code.")]
        public int MaxTradeCode { get; set; } = 8199;

        [Category(Dumping), Description("Link Trade: Dumping routine will stop after a maximum number of dumps from a single user.")]
        public int MaxDumpsPerTrade { get; set; } = 20;

        [Category(Dumping), Description("Link Trade: Dumping routine will stop after spending x seconds in trade.")]
        public int MaxDumpTradeTime { get; set; } = 180;

        /// <summary>
        /// Gets a random trade code based on the range settings.
        /// </summary>
        public int GetRandomTradeCode() => Util.Rand.Next(MinTradeCode, MaxTradeCode + 1);
    }
}
