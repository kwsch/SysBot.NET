using System.ComponentModel;

namespace SysBot.Pokemon;

public class DiscordSettings
{
    private const string Startup = nameof(Startup);
    private const string Operation = nameof(Operation);
    private const string Channels = nameof(Channels);
    private const string Roles = nameof(Roles);
    private const string Users = nameof(Users);
    public override string ToString() => "Einstellungen zur Discord-Integration";

    // Startup

    [Category(Startup), Description("Bot login token.")]
    public string Token { get; set; } = string.Empty;

    [Category(Startup), Description("Bot-Befehls-Präfix.")]
    public string CommandPrefix { get; set; } = "$";

    [Category(Startup), Description("Liste der Module, die beim Start des Bots nicht geladen werden (durch Kommas getrennt).")]
    public string ModuleBlacklist { get; set; } = string.Empty;

    [Category(Startup), Description("Schalten Sie um, um Befehle asynchron oder synchron zu verarbeiten.")]
    public bool AsyncCommands { get; set; }

    [Category(Startup), Description("Benutzerdefinierter Status für das Spielen.")]
    public string BotGameStatus { get; set; } = "SysBot.German: Pokémon";

    [Category(Startup), Description("Zeigt die Farbe des Discord-Präsenzstatus an, wobei nur Bots vom Typ Handel berücksichtigt werden.")]
    public bool BotColorStatusTradeOnly { get; set; } = true;

    [Category(Operation), Description("Benutzerdefinierte Nachricht, mit der der Bot antwortet, wenn ein Benutzer ihn grüßt. Verwenden Sie die String-Formatierung, um den Benutzer in der Antwort zu erwähnen.")]
    public string HelloResponse { get; set; } = "Hi {0}!";

    // Whitelists

    [Category(Roles), Description("Benutzer mit dieser Rolle sind berechtigt, die Warteschlange für den Handel zu betreten.")]
    public RemoteControlAccessList RoleCanTrade { get; set; } = new() { AllowIfEmpty = false };

    [Category(Roles), Description("Benutzer mit dieser Rolle dürfen die Warteschlange für die Seed-Prüfung betreten.")]
    public RemoteControlAccessList RoleCanSeedCheck { get; set; } = new() { AllowIfEmpty = false };

    [Category(Roles), Description("Benutzer mit dieser Rolle sind berechtigt, die Klon-Warteschlange zu betreten.")]
    public RemoteControlAccessList RoleCanClone { get; set; } = new() { AllowIfEmpty = false };

    [Category(Roles), Description("Benutzer mit dieser Rolle sind berechtigt, die Dump-Warteschlange zu betreten.")]
    public RemoteControlAccessList RoleCanDump { get; set; } = new() { AllowIfEmpty = false };

    [Category(Roles), Description("Benutzer mit dieser Rolle sind berechtigt, die Konsole fernzusteuern (wenn sie als Remote Control Bot ausgeführt wird).")]
    public RemoteControlAccessList RoleRemoteControl { get; set; } = new() { AllowIfEmpty = false };

    [Category(Roles), Description("Benutzer mit dieser Rolle können die Befehlsbeschränkungen umgehen.")]
    public RemoteControlAccessList RoleSudo { get; set; } = new() { AllowIfEmpty = false };

    // Operation

    [Category(Roles), Description("Benutzer mit dieser Rolle dürfen sich mit einer besseren Position in die Warteschlange einreihen.")]
    public RemoteControlAccessList RoleFavored { get; set; } = new() { AllowIfEmpty = false };

    [Category(Users), Description("Benutzer mit diesen Benutzer-IDs können den Bot nicht verwenden.")]
    public RemoteControlAccessList UserBlacklist { get; set; } = new();

    [Category(Channels), Description("Kanäle mit diesen IDs sind die einzigen Kanäle, auf denen der Bot Befehle annimmt.")]
    public RemoteControlAccessList ChannelWhitelist { get; set; } = new();

    [Category(Users), Description("Durch Kommas getrennte Discord-Benutzer-IDs, die sudo-Zugriff auf den Bot Hub haben werden.")]
    public RemoteControlAccessList GlobalSudoList { get; set; } = new();

    [Category(Users), Description("Wenn Sie dies deaktivieren, wird die globale sudo-Unterstützung deaktiviert.")]
    public bool AllowGlobalSudo { get; set; } = true;

    [Category(Channels), Description("Kanal-IDs, die ein Echo der Log-Bot-Daten ausgeben werden.")]
    public RemoteControlAccessList LoggingChannels { get; set; } = new();

    [Category(Channels), Description("Logger-Kanäle, die Meldungen zum Handelsstart protokollieren.")]
    public RemoteControlAccessList TradeStartingChannels { get; set; } = new();

    [Category(Channels), Description("Echo-Kanäle, die spezielle Meldungen protokollieren.")]
    public RemoteControlAccessList EchoChannels { get; set; } = new();

    [Category(Operation), Description("Gibt dem Benutzer PKM-Dateien von Pokémon aus dem Handel zurück.")]
    public bool ReturnPKMs { get; set; } = true;

    [Category(Operation), Description("Antwortet Benutzern, wenn sie einen bestimmten Befehl im Kanal nicht verwenden dürfen. Bei false ignoriert der Bot sie stattdessen stillschweigend.")]
    public bool ReplyCannotUseCommandInChannel { get; set; } = true;

    [Category(Operation), Description("Der Bot hört auf Kanalnachrichten und antwortet mit einem ShowdownSet, wenn eine PKM-Datei angehängt wird (nicht mit einem Befehl).")]
    public bool ConvertPKMToShowdownSet { get; set; } = true;

    [Category(Operation), Description("Der Bot kann in jedem Kanal, den der Bot sehen kann, mit einem ShowdownSet antworten, anstatt nur in Kanälen, für die der Bot auf der Whitelist steht. Aktiviere diese Option nur, wenn du möchtest, dass der Bot in Nicht-Bot-Channels mehr Nutzen bringt.")]
    public bool ConvertPKMReplyAnyChannel { get; set; }
}
