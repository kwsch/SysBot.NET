using System.ComponentModel;
using System.IO;
using PKHeX.Core;

namespace SysBot.Pokemon
{
    public sealed class PokeTradeHubConfig : IDumper
    {
        private const string Files = nameof(Files);
        private const string TradeCode = nameof(TradeCode);

        [Category(Files), Description("Source folder: where PKM files to distribute are selected from.")]
        public string DistributeFolder { get; set; } = string.Empty;

        [Category(Files), Description("Destination folder: where all received PKM files are dumped to.")]
        public string DumpFolder { get; set; } = string.Empty;

        [Category(Files), Description("Destination folder: where all received PKM files are dumped to.")]
        public bool Dump { get; set; }

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