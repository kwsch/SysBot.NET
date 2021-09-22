using System.ComponentModel;

// ReSharper disable AutoPropertyCanBeMadeGetOnly.Global

namespace SysBot.Pokemon
{
    public sealed class PokeTradeHubConfig : BaseConfig
    {
        private const string Bots = nameof(Bots);
        private const string Integration = nameof(Integration);

        [Browsable(false)]
        public override bool Shuffled => Distribution.Shuffled;

        [Category(FeatureToggle), Description("Method for detecting the overworld. Original method may not work consistently for some users, while ConsoleLanguageSpecific method requires your Switch console language.")]
        public ScreenDetectionMode ScreenDetection { get; set; } = ScreenDetectionMode.ConsoleLanguageSpecific;

        [Category(FeatureToggle), Description("ConsoleLanguageSpecific screen detection method only. Set your Switch console language here for bots to work properly. All consoles should be using the same language.")]
        public ConsoleLanguageParameter ConsoleLanguage { get; set; }

        [Category(Operation)]
        [TypeConverter(typeof(ExpandableObjectConverter))]
        public QueueSettings Queues { get; set; } = new();

        [Category(Operation), Description("Stop conditions for EggBot, FossilBot, and EncounterBot.")]
        [TypeConverter(typeof(ExpandableObjectConverter))]
        public StopConditionSettings StopConditions { get; set; } = new();

        [Category(Operation), Description("Add extra time for slower Switches.")]
        [TypeConverter(typeof(ExpandableObjectConverter))]
        public TimingSettings Timings { get; set; } = new();

        // Bots

        [Category(Bots)]
        [TypeConverter(typeof(ExpandableObjectConverter))]
        public TradeSettings Trade { get; set; } = new();

        [Category(Bots)]
        [TypeConverter(typeof(ExpandableObjectConverter))]
        public SeedCheckSettings SeedCheck { get; set; } = new();

        [Category(Bots), Description("Settings for idle distribution trades.")]
        [TypeConverter(typeof(ExpandableObjectConverter))]
        public DistributionSettings Distribution { get; set; } = new();

        [Category(Bots)]
        [TypeConverter(typeof(ExpandableObjectConverter))]
        public RaidSettings Raid { get; set; } = new();

        [Category(Bots)]
        [TypeConverter(typeof(ExpandableObjectConverter))]
        public EggSettings Egg { get; set; } = new();

        [Category(Bots)]
        [TypeConverter(typeof(ExpandableObjectConverter))]
        public FossilSettings Fossil { get; set; } = new();

        [Category(Bots)]
        [TypeConverter(typeof(ExpandableObjectConverter))]
        public EncounterSettings Encounter { get; set; } = new();

        // Integration

        [Category(Integration)]
        [TypeConverter(typeof(ExpandableObjectConverter))]
        public DiscordSettings Discord { get; set; } = new();

        [Category(Integration)]
        [TypeConverter(typeof(ExpandableObjectConverter))]
        public TwitchSettings Twitch { get; set; } = new();

        [Category(Integration)]
        [TypeConverter(typeof(ExpandableObjectConverter))]
        public YouTubeSettings YouTube { get; set; } = new();

        [Category(Integration), Description("Configure generation of assets for streaming.")]
        [TypeConverter(typeof(ExpandableObjectConverter))]
        public StreamSettings Stream { get; set; } = new();

        [Category(Integration), Description("Allows favored users to join the queue with a more favorable position than unfavored users.")]
        [TypeConverter(typeof(ExpandableObjectConverter))]
        public FavoredPrioritySettings Favoritism { get; set; } = new();
    }
}