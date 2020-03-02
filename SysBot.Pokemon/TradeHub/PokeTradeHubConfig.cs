using System.ComponentModel;

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

        [Category(FeatureToggle), Description("When set, the bot will only send a Pokémon if it is legal!")]
        public bool VerifyLegality { get; set; } = true;

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
        public DuduSettings Dudu { get; set; } = new DuduSettings();

        [Category(Bots)]
        [TypeConverter(typeof(ExpandableObjectConverter))]
        public EggSettings Egg { get; set; } = new EggSettings();

        [Category(Bots)]
        [TypeConverter(typeof(ExpandableObjectConverter))]
        public FossilSettings Fossil { get; set; } = new FossilSettings();

        [Category(Bots)]
        [TypeConverter(typeof(ExpandableObjectConverter))]
        public RaidSettings Raid { get; set; } = new RaidSettings();

        // Integration

        [Category(Integration)]
        [TypeConverter(typeof(ExpandableObjectConverter))]
        public DiscordSettings Discord { get; set; } = new DiscordSettings();

        [Category(Integration)]
        [TypeConverter(typeof(ExpandableObjectConverter))]
        public TwitchSettings Twitch { get; set; } = new TwitchSettings();

#if DEBUG
        // Debug

        [Category(Debug), Description("Skips creating bots when the program is started; helpful for testing integrations.")]
        public bool SkipConsoleBotCreation { get; set; }
#endif
    }
}