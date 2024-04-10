using System;
using System.ComponentModel;
using System.Linq;

namespace SysBot.Pokemon;

public class TwitchSettings
{
    private const string Startup = nameof(Startup);
    private const string Operation = nameof(Operation);
    private const string Messages = nameof(Messages);
    public override string ToString() => "Einstellungen zur Twitch-Integration";

    // Startup

    [Category(Startup), Description("Bot Login Token")]
    public string Token { get; set; } = string.Empty;

    [Category(Startup), Description("Bot Username")]
    public string Username { get; set; } = string.Empty;

    [Category(Startup), Description("Kanal zum Senden von Nachrichten an")]
    public string Channel { get; set; } = string.Empty;

    [Category(Startup), Description("Bot-Befehls-Präfix")]
    public char CommandPrefix { get; set; } = '$';

    [Category(Operation), Description("Nachricht, die gesendet wird, wenn die Schranke aufgehoben wird.")]
    public string MessageStart { get; set; } = string.Empty;

    // Messaging

    [Category(Operation), Description("Den Bot vom Senden von Nachrichten abhalten, wenn in den letzten Y Sekunden X Nachrichten gesendet wurden.")]
    public int ThrottleMessages { get; set; } = 100;

    [Category(Operation), Description("Den Bot vom Senden von Nachrichten abhalten, wenn in den letzten Y Sekunden X Nachrichten gesendet wurden.")]
    public double ThrottleSeconds { get; set; } = 30;

    [Category(Operation), Description("Den Bot daran hindern, Flüstern zu senden, wenn in den letzten Y Sekunden X Nachrichten gesendet wurden.")]
    public int ThrottleWhispers { get; set; } = 100;

    [Category(Operation), Description("Den Bot daran hindern, Flüstern zu senden, wenn in den letzten Y Sekunden X Nachrichten gesendet wurden.")]
    public double ThrottleWhispersSeconds { get; set; } = 60;

    // Operation

    [Category(Operation), Description("Sudo Usernames")]
    public string SudoList { get; set; } = string.Empty;

    [Category(Operation), Description("Benutzer mit diesen Benutzernamen können den Bot nicht verwenden.")]
    public string UserBlacklist { get; set; } = string.Empty;

    [Category(Operation), Description("Wenn diese Option aktiviert ist, verarbeitet der Bot Befehle, die an den Kanal gesendet werden.")]
    public bool AllowCommandsViaChannel { get; set; } = true;

    [Category(Operation), Description("Wenn aktiviert, erlaubt der Bot den Benutzern, Befehle per Flüstern zu senden (umgeht den langsamen Modus)")]
    public bool AllowCommandsViaWhisper { get; set; }

    // Message Destinations

    [Category(Messages), Description("Legt fest, wohin allgemeine Benachrichtigungen gesendet werden.")]
    public TwitchMessageDestination NotifyDestination { get; set; }

    [Category(Messages), Description("Legt fest, wohin TradeStart-Benachrichtigungen gesendet werden.")]
    public TwitchMessageDestination TradeStartDestination { get; set; } = TwitchMessageDestination.Channel;

    [Category(Messages), Description("Legt fest, wohin TradeSearch-Benachrichtigungen gesendet werden.")]
    public TwitchMessageDestination TradeSearchDestination { get; set; }

    [Category(Messages), Description("Legt fest, wohin TradeFinish-Benachrichtigungen gesendet werden.")]
    public TwitchMessageDestination TradeFinishDestination { get; set; }

    [Category(Messages), Description("Legt fest, wohin TradeCancled-Benachrichtigungen gesendet werden.")]
    public TwitchMessageDestination TradeCanceledDestination { get; set; } = TwitchMessageDestination.Channel;

    [Category(Messages), Description("Legt fest, ob der Handel der Verteilung vor dem Start abwärts zählt.")]
    public bool DistributionCountDown { get; set; } = true;

    public bool IsSudo(string username)
    {
        var sudos = SudoList.Split([ ",", ", ", " " ], StringSplitOptions.RemoveEmptyEntries);
        return sudos.Contains(username);
    }
}

public enum TwitchMessageDestination
{
    Disabled,
    Channel,
    Whisper,
}
