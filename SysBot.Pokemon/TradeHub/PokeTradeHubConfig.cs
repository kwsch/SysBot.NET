using System.ComponentModel;
using SysBot.Base;

// ReSharper disable AutoPropertyCanBeMadeGetOnly.Global

namespace SysBot.Pokemon
{
    public sealed class PokeTradeHubConfig
    {
        private const string FeatureToggle = nameof(FeatureToggle);
        private const string Operation = nameof(Operation);
        private const string Bots = nameof(Bots);
        private const string Integration = nameof(Integration);
        private const string Debug = nameof(Debug);

        [Category(FeatureToggle), Description("When enabled, the bot will press the B button occasionally when it is not processing anything (to avoid sleep).")]
        public bool AntiIdle { get; set; }

        [Category(FeatureToggle), Description("Method for detecting the overworld. Original method may not work consistently for some users, while ConsoleLanguageSpecific method requires your Switch console language.")]
        public ScreenDetectionMode ScreenDetection { get; set; }

        [Category(FeatureToggle), Description("ConsoleLanguageSpecific screen detection method only. Set your Switch console language here for bots to work properly. All consoles should be using the same language.")]
        public ConsoleLanguageParameter ConsoleLanguage { get; set; }

        [Category(FeatureToggle), Description("Holds Capture button to record a 30 second clip when a matching shiny Pokémon is found by EncounterBot or Fossilbot.")]
        public bool CaptureVideoClip { get; set; }

        [Category(FeatureToggle), Description("Extra time in milliseconds to wait after clicking + to reconnect to Y-Comm.")]
        public int ExtraTimeReconnectYComm { get; set; } = 0;

        [Category(Operation)]
        [TypeConverter(typeof(ExpandableObjectConverter))]
        public DistributionSettings Distribute { get; set; } = new DistributionSettings();

        [Category(Operation)]
        [TypeConverter(typeof(ExpandableObjectConverter))]
        public CountSettings Counts { get; set; } = new CountSettings();

        [Category(Operation)]
        [TypeConverter(typeof(ExpandableObjectConverter))]
        public LegalitySettings Legality { get; set; } = new LegalitySettings();

        [Category(Operation)]
        [TypeConverter(typeof(ExpandableObjectConverter))]
        public FolderSettings Folder { get; set; } = new FolderSettings();

        [Category(Operation)]
        [TypeConverter(typeof(ExpandableObjectConverter))]
        public QueueSettings Queues { get; set; } = new QueueSettings();

        // Bots

        [Category(Bots)]
        [TypeConverter(typeof(ExpandableObjectConverter))]
        public TradeSettings Trade { get; set; } = new TradeSettings();

        [Category(Bots)]
        [TypeConverter(typeof(ExpandableObjectConverter))]
        public SeedCheckSettings SeedCheck { get; set; } = new SeedCheckSettings();

        [Category(Bots)]
        [TypeConverter(typeof(ExpandableObjectConverter))]
        public EggSettings Egg { get; set; } = new EggSettings();

        [Category(Bots)]
        [TypeConverter(typeof(ExpandableObjectConverter))]
        public FossilSettings Fossil { get; set; } = new FossilSettings();

        [Category(Bots)]
        [TypeConverter(typeof(ExpandableObjectConverter))]
        public RaidSettings Raid { get; set; } = new RaidSettings();

        [Category(Bots)]
        [TypeConverter(typeof(ExpandableObjectConverter))]
        public EncounterSettings Encounter { get; set; } = new EncounterSettings();

        // Integration

        [Category(Integration)]
        [TypeConverter(typeof(ExpandableObjectConverter))]
        public DiscordSettings Discord { get; set; } = new DiscordSettings();

        [Category(Integration)]
        [TypeConverter(typeof(ExpandableObjectConverter))]
        public TwitchSettings Twitch { get; set; } = new TwitchSettings();

        [Category(Integration)]
        [TypeConverter(typeof(ExpandableObjectConverter))]
        public StreamSettings Stream { get; set; } = new StreamSettings();

        [Category(Debug), Description("Skips creating bots when the program is started; helpful for testing integrations.")]
        public bool SkipConsoleBotCreation { get; set; }
    }
}