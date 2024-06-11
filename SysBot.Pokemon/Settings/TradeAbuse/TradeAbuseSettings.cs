using System.ComponentModel;

namespace SysBot.Pokemon;

public class TradeAbuseSettings
{
    private const string Monitoring = nameof(Monitoring);

    [Category(Monitoring), Description("Banned online IDs that will trigger trade exit or in-game block.")]
    public RemoteControlAccessList BannedIDs { get; set; } = new();

    [Category(Monitoring), Description("When a person using Ledy nickname swaps is detected of abuse, the echo message will include their Nintendo Account ID.")]
    public bool EchoNintendoOnlineIDLedy { get; set; } = true;

    [Category(Monitoring), Description("If not empty, the provided string will be appended to Echo alerts to notify whomever you specify when a user violates Ledy trade rules. For Discord, use <@userIDnumber> to mention.")]
    public string LedyAbuseEchoMention { get; set; } = string.Empty;

    [Category(Monitoring), Description("When a person appears with a different Discord/Twitch account in less than this setting's value (minutes), a notification will be sent.")]
    public double TradeAbuseExpiration { get; set; } = 10;

    [Category(Monitoring), Description("When a person appears again in less than this setting's value (minutes), a notification will be sent.")]
    public double TradeCooldown { get; set; }

    public override string ToString() => "Trade Abuse Monitoring Settings";
}
