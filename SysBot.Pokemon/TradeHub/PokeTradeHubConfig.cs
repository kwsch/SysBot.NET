using System.ComponentModel;

// ReSharper disable AutoPropertyCanBeMadeGetOnly.Global

namespace SysBot.Pokemon;

public sealed class PokeTradeHubConfig : BaseConfig
{
    private const string BotTrade = nameof(BotTrade);
    private const string BotEncounter = nameof(BotEncounter);
    private const string Integration = nameof(Integration);

    [Browsable(false)]
    public override bool Shuffled => Distribution.Shuffled;

    [Category(Operation), Description("")]
    [TypeConverter(typeof(ExpandableObjectConverter))]
    public QueueSettings Queues { get; set; } = new();

    [Category(Operation), Description("Fügen Sie zusätzliche Zeit für langsamere Switches hinzu.")]
    [TypeConverter(typeof(ExpandableObjectConverter))]
    public TimingSettings Timings { get; set; } = new();

    // Trade Bots

    [Category(BotTrade), Description("HandelsBot Einstellungen")]
    [TypeConverter(typeof(ExpandableObjectConverter))]
    public TradeSettings Trade { get; set; } = new();

    [Category(BotTrade), Description("Einstellungen für den Leerlaufhandel.")]
    [TypeConverter(typeof(ExpandableObjectConverter))]
    public DistributionSettings Distribution { get; set; } = new();

    [Category(BotTrade), Description("")]
    [TypeConverter(typeof(ExpandableObjectConverter))]
    public SeedCheckSettings SeedCheckSWSH { get; set; } = new();

    [Category(BotTrade), Description("Einstellungen zur Überwachung des Handelsmissbrauchs")]
    [TypeConverter(typeof(ExpandableObjectConverter))]
    public TradeAbuseSettings TradeAbuse { get; set; } = new();

    // Encounter Bots - For finding or hosting Pokémon in-game.

    [Category(BotEncounter), Description("")]
    [TypeConverter(typeof(ExpandableObjectConverter))]
    public EncounterSettings EncounterSWSH { get; set; } = new();

    [Category(BotEncounter), Description("")]
    [TypeConverter(typeof(ExpandableObjectConverter))]
    public RaidSettings RaidSWSH { get; set; } = new();

    [Category(BotEncounter), Description("Stoppbedingungen für EncounterBot.")]
    [TypeConverter(typeof(ExpandableObjectConverter))]
    public StopConditionSettings StopConditions { get; set; } = new();

    // Integration

    [Category(Integration), Description("Einstellungen für Discord")]
    [TypeConverter(typeof(ExpandableObjectConverter))]
    public DiscordSettings Discord { get; set; } = new();

    [Category(Integration), Description("Einstellungen für Twitch")]
    [TypeConverter(typeof(ExpandableObjectConverter))]
    public TwitchSettings Twitch { get; set; } = new();

    [Category(Integration), Description("Einstellungen für Youtube")]
    [TypeConverter(typeof(ExpandableObjectConverter))]
    public YouTubeSettings YouTube { get; set; } = new();

    [Category(Integration), Description("Konfigurieren Sie die Generierung von Assets für das Streaming.")]
    [TypeConverter(typeof(ExpandableObjectConverter))]
    public StreamSettings Stream { get; set; } = new();

    [Category(Integration), Description("Ermöglicht es bevorzugten Benutzern, sich mit einer günstigeren Position in die Warteschlange einzureihen als nicht bevorzugte Benutzer.")]
    [TypeConverter(typeof(ExpandableObjectConverter))]
    public FavoredPrioritySettings Favoritism { get; set; } = new();
}
