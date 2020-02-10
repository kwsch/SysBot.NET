using System.ComponentModel;
using System.IO;
using PKHeX.Core;

namespace SysBot.Pokemon
{
    public sealed class PokeTradeHubConfig : IDumper
    {
        private const string FeatureToggle = nameof(FeatureToggle);
        private const string Files = nameof(Files);
        private const string TradeCode = nameof(TradeCode);

        #region Toggles
        [Category(FeatureToggle), Description("Destination folder: where all received PKM files are dumped to.")]
        public bool Dump { get; set; }

        [Category(FeatureToggle), Description("Link Trade: Distributes PKM files when idle from the DistributeFolder.")]
        public bool DistributeWhileIdle { get; set; } = true;

        [Category(FeatureToggle), Description("Link Trade: Enables trading priority files sourced from the priority folder.")]
        public bool MonitorForPriorityTrades { get; set; }

        [Category(FeatureToggle), Description("Link Trade: Using multiple bots -- all bots will confirm their trade code at the same time.")]
        public bool SynchronizeLinkTradeBots { get; set; } = true;
        #endregion

        #region Folders
        [Category(Files), Description("Source folder: where PKM files to distribute are selected from.")]
        public string DistributeFolder { get; set; } = string.Empty;

        [Category(Files), Description("Destination folder: where all received PKM files are dumped to.")]
        public string DumpFolder { get; set; } = string.Empty;

        [Category(Files), Description("Link Trade: where priority PKM details to distribute are selected from.")]
        public string PriorityFolder { get; set; } = string.Empty;
        #endregion

        #region Trade Codes
        /// <summary>
        /// Minimum trade code to be yielded.
        /// </summary>
        [Category(TradeCode), Description("Minimum Link Code.")]
        public int MinTradeCode { get; set; } = 8180;

        /// <summary>
        /// Maximum trade code to be yielded.
        /// </summary>
        [Category(TradeCode), Description("Maximum Link Code.")]
        public int MaxTradeCode { get; set; } = 8199;

        /// <summary>
        /// Amount of Trades that have been completed.
        /// </summary>
        [Category(TradeCode), Description("Completed Trades.")]
        public int CompletedTrades { get; set; }

        /// <summary>
        /// Gets a random trade code based on the range settings.
        /// </summary>
        public int GetRandomTradeCode() => Util.Rand.Next(MinTradeCode, MaxTradeCode + 1);
        #endregion

        public void CreateDefaults(string path)
        {
            var dump = Path.Combine(path, "dump");
            Directory.CreateDirectory(dump);
            DumpFolder = dump;
            Dump = true;

            var distribute = Path.Combine(path, "distribute");
            Directory.CreateDirectory(distribute);
            DistributeFolder = distribute;
        }
    }
}