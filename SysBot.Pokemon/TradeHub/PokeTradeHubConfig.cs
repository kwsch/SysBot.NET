using System.ComponentModel;
using System.IO;
using PKHeX.Core;
using SysBot.Base;

// ReSharper disable AutoPropertyCanBeMadeGetOnly.Global

namespace SysBot.Pokemon
{
    public sealed class PokeTradeHubConfig : IDumper, IPoolSettings, ITwitchSettings, ISynchronizationSetting
    {
        private const string FeatureToggle = nameof(FeatureToggle);
        private const string Files = nameof(Files);
        private const string TradeCode = nameof(TradeCode);
        private const string IntegrationDiscord = nameof(IntegrationDiscord);
        private const string IntegrationTwitch = nameof(IntegrationTwitch);
        private const string Legality = nameof(Legality);
        private const string Metadata = nameof(Metadata);
        private const string FossilPokemon = nameof(FossilPokemon);

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

        [Category(FeatureToggle), Description("When set to something other than None, the Random Trades will require this species in addition to the nickname match.")]
        public Species DistributeLedySpecies { get; set; } = Species.Wooloo;

        [Category(FeatureToggle), Description("When enabled, the EggBot will continue to get eggs and dump the Pokémon into the egg dump folder")]
        public bool ContinueGettingEggs { get; set; } = false;

        [Category(FeatureToggle), Description("Link Trade: Using multiple distribution bots -- all bots will confirm their trade code at the same time. When Local, the bots will continue when all are at the barrier. When Remote, something else must signal the bots to continue.")]
        public BotSyncOption SynchronizeBots { get; set; } = BotSyncOption.LocalSync;

        [Category(FeatureToggle), Description("When set, the bot will assume that ldn_mitm sysmodule is running on your system. Better stability")]
        public bool UseLdnMitm { get; set; } = true;

        [Category(Legality), Description("Link Trade: Using multiple distribution bots -- once all bots are ready to confirm trade code, the Hub will wait X milliseconds before releasing all bots.")]
        public int SynchronizeDelayBarrier { get; set; }

        [Category(Legality), Description("Link Trade: Using multiple distribution bots -- how long (Seconds) a bot will wait for synchronization before continuing anyways.")]
        public double SynchronizeTimeout { get; set; } = 90;

        [Category(Legality), Description("Link Trade: Dumping routine will stop after a maximum number of dumps from a single user.")]
        public int MaxDumpsPerTrade { get; set; } = 20;

        [Category(Legality), Description("Link Trade: Dumping routine will stop after spending x seconds in trade.")]
        public int MaxDumpTradeTime { get; set; } = 180;
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
        [Category(TradeCode), Description("Minimum Link Code.")]
        public int MinTradeCode { get; set; } = 8180;

        [Category(TradeCode), Description("Maximum Link Code.")]
        public int MaxTradeCode { get; set; } = 8199;

        [Category(TradeCode), Description("Distribution Trade Link Code.")]
        public int DistributionTradeCode { get; set; } = 7196;

        [Category(TradeCode), Description("Distribution Trade Link Code uses the Min and Max range rather than the fixed trade code.")]
        public bool DistributionRandomCode { get; set; }

        /// <summary>
        /// Gets a random trade code based on the range settings.
        /// </summary>
        public int GetRandomTradeCode() => Util.Rand.Next(MinTradeCode, MaxTradeCode + 1);
        #endregion

        #region Counts
        [Category(Metadata), Description("Completed Surprise Trades")]
        public int CompletedSurprise { get; set; }

        [Category(Metadata), Description("Completed Link Trades (Distribution)")]
        public int CompletedDistribution { get; set; }

        [Category(Metadata), Description("Completed Link Trades (Specific User)")]
        public int CompletedTrades { get; set; }

        [Category(Metadata), Description("Completed Dudu Trades")]
        public int CompletedDudu { get; set; }

        [Category(Metadata), Description("Completed Clone Trades (Specific User)")]
        public int CompletedClones { get; set; }

        [Category(Metadata), Description("Completed Dump Trades (Specific User)")]
        public int CompletedDumps { get; set; }

        [Category(Metadata), Description("Eggs Retrieved")]
        public int CompletedEggs { get; set; }

        [Category(Metadata), Description("Fossil Pokémon Revived")]
        public int CompletedFossils { get; set; }

        [Category(Metadata), Description("Raids Completed")]
        public int CompletedRaids { get; set; }
        #endregion

        #region Integration
        private const string DefaultDisable = "DISABLE";

        [Category(IntegrationDiscord), Description("Discord Bot: Bot Login Token")]
        public string DiscordToken { get; set; } = string.Empty;

        [Category(IntegrationDiscord), Description("Discord Bot: Bot Command Prefix")]
        public string DiscordCommandPrefix { get; set; } = "$";

        [Category(IntegrationDiscord), Description("Discord Bot: List of Modules that will not be loaded when the Bot is started (comma separated).")]
        public string DiscordModuleBlacklist { get; set; } = string.Empty;

        [Category(IntegrationDiscord), Description("Discord Bot: Users with this role are allowed to enter the Trade queue.")]
        public string DiscordRoleCanTrade { get; set; } = DefaultDisable;

        [Category(IntegrationDiscord), Description("Discord Bot: Users with this role are allowed to enter the Dudu queue.")]
        public string DiscordRoleCanDudu { get; set; } = DefaultDisable;

        [Category(IntegrationDiscord), Description("Discord Bot: Users with this role are allowed to enter the Clone queue.")]
        public string DiscordRoleCanClone { get; set; } = DefaultDisable;

        [Category(IntegrationDiscord), Description("Discord Bot: Users with this role are allowed to enter the Dump queue.")]
        public string DiscordRoleCanDump { get; set; } = DefaultDisable;

        [Category(IntegrationDiscord), Description("Discord Bot: Users with this role are allowed to bypass command restrictions.")]
        public string DiscordRoleSudo { get; set; } = DefaultDisable;

        [Category(IntegrationDiscord), Description("Discord Bot: Users with these user IDs cannot use the bot.")]
        public string DiscordBlackList { get; set; } = string.Empty;

        [Category(IntegrationDiscord), Description("Discord Bot: Channels with these IDs are the only channels where the bot acknowledges commands.")]
        public string DiscordWhiteList { get; set; } = string.Empty;

        [Category(IntegrationDiscord), Description("Discord Bot: Custom Status for playing a game.")]
        public string DiscordGameStatus { get; set; } = "SysBot.NET: Pokémon";

        [Category(IntegrationDiscord), Description("Global Sudo: Disabling this will remove global sudo support.")]
        public bool AllowGlobalSudo { get; set; } = true;

        [Category(IntegrationDiscord), Description("Global Sudo List: Comma separated Discord user IDs that will have sudo access to the Bot Hub.")]
        public string GlobalSudoList { get; set; } = string.Empty;

        [Category(IntegrationDiscord), Description("Global Loggers: Comma separated Logger channel IDs that will persistently log bot data.")]
        public string GlobalDiscordLoggers { get; set; } = string.Empty;

        [Category(IntegrationTwitch), Description("Twitch Bot: Bot Login Token")]
        public string TwitchToken { get; set; } = string.Empty;

        [Category(IntegrationTwitch), Description("Twitch Bot: Bot Username")]
        public string TwitchUsername { get; set; } = string.Empty;

        [Category(IntegrationTwitch), Description("Twitch Bot: Channel to Send Messages To")]
        public string TwitchChannel { get; set; } = string.Empty;

        [Category(IntegrationTwitch), Description("Twitch Bot: Message sent when the Barrier is released.")]
        public string TwitchMessageStart { get; set; } = string.Empty;

        [Category(IntegrationTwitch), Description("Twitch Bot: Sudo Usernames")]
        public string TwitchSudoList { get; set; } = string.Empty;

        [Category(IntegrationTwitch), Description("Twitch Bot: Users with these usernames cannot use the bot.")]
        public string TwitchBlackList { get; set; } = string.Empty;

        [Category(IntegrationTwitch), Description("Twitch Bot: Sub only mode (Restricts the bot to twitch subs only)")]
        public bool SubOnlyBot { get; set; } = false;
        #endregion

        #region Legality
        [Category(Legality), Description("Legality: Regenerated PKM files will attempt to be sourced from games using trainer data info from these PKM Files.")]
        public string GeneratePathTrainerInfo { get; set; } = string.Empty;

        [Category(Legality), Description("Legality: Default Trainer Name for PKM files that can't originate from any of the provided SaveFiles.")]
        public string GenerateOT { get; set; } = "SysBot";

        [Category(Legality), Description("Legality: Default 16 Bit Trainer ID (TID) for PKM files that can't originate from any of the provided SaveFiles.")]
        public int GenerateTID16 { get; set; } = 12345;

        [Category(Legality), Description("Legality: Default 16 Bit Secret ID (SID) for PKM files that can't originate from any of the provided SaveFiles.")]
        public int GenerateSID16 { get; set; } = 54321;

        [Category(Legality), Description("Legality: Default Language for PKM files that can't originate from any of the provided SaveFiles.")]
        public LanguageID GenerateLanguage { get; set; } = LanguageID.English;

        [Category(Legality), Description("Legality: Set all possible ribbons for any generated Pokémon.")]
        public bool SetAllLegalRibbons { get; set; }

        [Category(Legality), Description("Legality: Set a matching ball (based on color) for any generated Pokémon.")]
        public bool SetMatchingBalls { get; set; }

        [Category(Legality), Description("Legality: Zero out HOME tracker regardless of current tracker value. Applies to user requested PKM files as well.")]
        public bool ResetHOMETracker { get; set; } = true;

        #endregion

        #region Fossil Pokemon
        /// <summary>
        /// Species of fossil Pokemon to hunt for.
        /// </summary>
        [Category(FossilPokemon), Description("Species of fossil Pokémon to hunt for.")]
        public FossilSpecies FossilSpecies { get; set; } = FossilSpecies.Dracozolt;

        /// <summary>
        /// Toggle for injecting fossil pieces.
        /// </summary>
        [Category(FossilPokemon), Description("Toggle for injecting fossil pieces.")]
        public bool InjectFossils { get; set; } = false;

        /// <summary>
        /// Toggle for continuing to revive fossils after condition has been met.
        /// </summary>
        [Category(FossilPokemon), Description("When enabled, the FossilBot will continue to get fossils and dump the Pokémon into the fossil dump folder.")]
        public bool ContinueGettingFossils { get; set; } = false;

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

    public interface ITwitchSettings
    {
        string TwitchToken { get; }

        string TwitchUsername { get; }

        string TwitchChannel { get; }
        string TwitchMessageStart { get; }
    }
}