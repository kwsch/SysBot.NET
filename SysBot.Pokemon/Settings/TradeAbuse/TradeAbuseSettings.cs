using System.ComponentModel;

namespace SysBot.Pokemon;

public class TradeAbuseSettings
{
    private const string Monitoring = nameof(Monitoring);
    public override string ToString() => "Einstellungen zur Überwachung des Handelsmissbrauchs";

    [Category(Monitoring), Description("Wenn eine Person in weniger als der hier eingestellten Zeit (Minuten) wieder auftaucht, wird eine Benachrichtigung gesendet.")]
    public double TradeCooldown { get; set; }

    [Category(Monitoring), Description("Wenn eine Person eine Handelspause ignoriert, enthält die Echo-Nachricht ihre Nintendo-Konto-ID.")]
    public bool EchoNintendoOnlineIDCooldown { get; set; } = true;

    [Category(Monitoring), Description("Wenn die angegebene Zeichenkette nicht leer ist, wird sie an Echo-Benachrichtigungen angehängt, um die von Ihnen angegebene Person zu benachrichtigen, wenn ein Benutzer die Handelspausen nicht einhält. Für Discord, verwenden Sie <@userIDnumber> um zu erwähnen.")]
    public string CooldownAbuseEchoMention { get; set; } = string.Empty;

    [Category(Monitoring), Description("Wenn eine Person mit einem anderen Discord/Twitch-Konto in weniger als den hier eingestellten Minuten auftaucht, wird eine Benachrichtigung gesendet.")]
    public double TradeAbuseExpiration { get; set; } = 120;

    [Category(Monitoring), Description("Wenn eine Person, die mehrere Discord/Twitch-Konten nutzt, erkannt wird, enthält die Echo-Nachricht ihre Nintendo-Konto-ID.")]
    public bool EchoNintendoOnlineIDMulti { get; set; } = true;

    [Category(Monitoring), Description("Wenn eine Person, die an mehrere Konten im Spiel sendet, erkannt wird, enthält die Echo-Nachricht ihre Nintendo Account ID.")]
    public bool EchoNintendoOnlineIDMultiRecipients { get; set; } = true;

    [Category(Monitoring), Description("Wenn eine Person entdeckt wird, die mehrere Discord/Twitch-Konten verwendet, wird diese Maßnahme ergriffen.")]
    public TradeAbuseAction TradeAbuseAction { get; set; } = TradeAbuseAction.Quit;

    [Category(Monitoring), Description("Wenn eine Person im Spiel für mehrere Konten gesperrt wird, wird ihre Online-ID zu BannedIDs hinzugefügt.")]
    public bool BanIDWhenBlockingUser { get; set; } = true;

    [Category(Monitoring), Description("Wenn die angegebene Zeichenkette nicht leer ist, wird sie an die Echo-Warnungen angehängt, um die von Ihnen angegebene Person zu benachrichtigen, wenn ein Benutzer mit mehreren Konten gefunden wird. Für Discord verwenden Sie <@userIDnumber>, um zu erwähnen.")]
    public string MultiAbuseEchoMention { get; set; } = string.Empty;

    [Category(Monitoring), Description("Wenn die angegebene Zeichenkette nicht leer ist, wird sie an Echo-Benachrichtigungen angehängt, um die von Ihnen angegebene Person zu benachrichtigen, wenn ein Benutzer gefunden wird, der an mehrere Spieler im Spiel sendet. Für Discord, verwenden Sie <@userIDnumber> zum Erwähnen.")]
    public string MultiRecipientEchoMention { get; set; } = string.Empty;

    [Category(Monitoring), Description("Gebannte Online-IDs, die zum Abbruch des Handels oder zur Sperre im Spiel führen.")]
    public RemoteControlAccessList BannedIDs { get; set; } = new();

    [Category(Monitoring), Description("Wenn Sie auf eine Person mit einer gesperrten ID treffen, blockieren Sie sie im Spiel, bevor Sie den Handel beenden.")]
    public bool BlockDetectedBannedUser { get; set; } = true;

    [Category(Monitoring), Description("Wenn die angegebene Zeichenfolge nicht leer ist, wird sie an Echo-Benachrichtigungen angehängt, um die von Ihnen angegebene Person zu benachrichtigen, wenn ein Benutzer mit einer gesperrten ID übereinstimmt. Für Discord verwenden Sie <@userIDnumber>, um zu erwähnen.")]
    public string BannedIDMatchEchoMention { get; set; } = string.Empty;

    [Category(Monitoring), Description("Wenn eine Person, die Ledy-Nicknames tauscht, des Missbrauchs überführt wird, enthält die Echo-Nachricht ihre Nintendo-Konto-ID.")]
    public bool EchoNintendoOnlineIDLedy { get; set; } = true;

    [Category(Monitoring), Description("Wenn die angegebene Zeichenfolge nicht leer ist, wird sie an die Echo-Warnungen angehängt, um die von Ihnen angegebene Person zu benachrichtigen, wenn ein Benutzer gegen die Ledy-Handelsregeln verstößt. Für Discord verwenden Sie <@userIDnumber>, um zu erwähnen.")]
    public string LedyAbuseEchoMention { get; set; } = string.Empty;
}
