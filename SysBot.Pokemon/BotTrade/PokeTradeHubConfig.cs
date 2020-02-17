using System.ComponentModel;
using System.IO;
// ReSharper disable AutoPropertyCanBeMadeGetOnly.Global

namespace SysBot.Pokemon
{
    public sealed class PokeTradeHubConfig : IDumper, IPoolSettings
    {
        private const string FeatureToggle = nameof(FeatureToggle);
        private const string Files = nameof(Files);
        private const string TradeCode = nameof(TradeCode);
        private const string Integration = nameof(Integration);
        private const string Legality = nameof(Legality);

        #region Toggles
        [Category(FeatureToggle), Description("When enabled, dumps any received PKM files (trade results) to the DumpFolder.")]
        public bool Dump { get; set; }

        [Category(FeatureToggle), Description("When enabled, the bot will press the B button occasionally when it is not processing anything (to avoid sleep).")]
        public bool AntiIdle { get; set; }

        [Category(FeatureToggle), Description("When enabled, idle LinkTrade bots will randomly distribute PKM files from the DistributeFolder.")]
        public bool DistributeWhileIdle { get; set; } = true;

        [Category(FeatureToggle), Description("When enabled, the DistributionFolder will yield randomly rather than in the same sequence.")]
        public bool DistributeShuffled { get; set; }

        [Category(FeatureToggle), Description("When enabled, Dudu checks will return all possible seed results instead of the first valid match.")]
        public bool ShowAllZ3Results { get; set; }

        [Category(FeatureToggle), Description("Link Trade: Enables trading priority files sourced from the priority folder. This is not necessary if an integration service (e.g. Discord) is adding to the queue from the same executable process.")]
        public bool MonitorForPriorityTrades { get; set; }

        [Category(FeatureToggle), Description("Link Trade: Using multiple distribution bots -- all bots will confirm their trade code at the same time.")]
        public bool SynchronizeLinkTradeBots { get; set; } = true;

        [Category(Legality), Description("Link Trade: Using multiple distribution bots -- once all bots are ready to confirm trade code, the Hub will wait X milliseconds before releasing all bots.")]
        public int SynchronizeLinkTradeBotsDelay { get; set; }

        [Category(Legality), Description("Link Trade: Using multiple distribution bots -- how long (Seconds) a bot will wait for synchronization before continuing anyways.")]
        public double SynchronizeLinkTradeBotsTimeout { get; set; } = 60;
        #endregion

        #region Folders
        [Category(Files), Description("Source folder: where PKM files to distribute are selected from.")]
        public string DistributeFolder { get; set; } = string.Empty;

        [Category(Files), Description("Destination folder: where all received PKM files are dumped to.")]
        public string DumpFolder { get; set; } = string.Empty;

        [Category(Files), Description("Link Trade: where priority PKM details to distribute are selected from. This is not necessary if an integration service (e.g. Discord) is adding to the queue from the same executable process.")]
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
        public int GetRandomTradeCode() => PKHeX.Core.Util.Rand.Next(MinTradeCode, MaxTradeCode + 1);
        #endregion

        #region Integration
        private const string DefaultDisable = "DISABLE";

        [Category(Integration), Description("Discord Bot: Bot Login Token")]
        public string DiscordToken { get; set; } = string.Empty;

        [Category(Integration), Description("Discord Bot: Bot Command Prefix")]
        public string DiscordCommandPrefix { get; set; } = "$";

        [Category(Integration), Description("Discord Bot: Users with this role are allowed to enter the Trade queue.")]
        public string DiscordRoleCanTrade { get; set; } = DefaultDisable;

        [Category(Integration), Description("Discord Bot: Users with this role are allowed to enter the Dudu queue.")]
        public string DiscordRoleCanDudu { get; set; } = DefaultDisable;

        [Category(Integration), Description("Discord Bot: Users with this role are allowed to bypass command restrictions.")]
        public string DiscordRoleSudo { get; set; } = DefaultDisable;

        [Category(Integration), Description("Global Sudo: Disabling this will remove global sudo support.")]
        public bool AllowGlobalSudo { get; set; } = true;

        [Category(Integration), Description("Global Sudo List: Comma separated Discord user IDs that will have sudo access to the Bot Hub.")]
        public string GlobalSudoList { get; set; } = string.Empty;
        #endregion

        #region Legality
        [Category(Legality), Description("Legality: Regenerated PKM files will attempt to be sourced from games using trainer data info from these saves.")]
        public string GeneratePathSaveFiles { get; set; } = string.Empty;

        [Category(Legality), Description("Legality: Default Trainer Name for PKM files that can't originate from any of the provided SaveFiles.")]
        public string GenerateOT { get; set; } = "SysBot.NET";

        [Category(Legality), Description("Legality: Default 16 Bit Trainer ID (TID) for PKM files that can't originate from any of the provided SaveFiles.")]
        public int GenerateTID16 { get; set; }

        [Category(Legality), Description("Legality: Default 16 Bit Secret ID (SID) for PKM files that can't originate from any of the provided SaveFiles.")]
        public int GenerateSID16 { get; set; }

        [Category(Legality), Description("Legality: Default Language for PKM files that can't originate from any of the provided SaveFiles.")]
        public int GenerateLanguage { get; set; }

        [Category(Legality), Description("Legality: Set all possible ribbons for any generated Pokémon.")]
        public bool SetAllLegalRibbons { get; set; }

        [Category(Legality), Description("Legality: Set a matching ball (based on color) for any generated Pokémon.")]
        public bool SetMatchingBalls { get; set; }

        [Category(Legality), Description("Legality: Zero out HOME tracker regardless of current tracker value. Applies to user requested PKM files as well.")]
        public bool ResetHOMETracker { get; set; } = true;

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