using System.ComponentModel;

namespace SysBot.Pokemon;

public class TradeAbuseSettings
{
    private const string Monitoring = nameof(Monitoring);
    public override string ToString() => "Trade Abuse Monitoring Settings";

    [Category(Monitoring), Description("When a person appears again in less than this setting's value (minutes), a notification will be sent.")]
    public double TradeCooldown { get; set; }

    [Category(Monitoring), Description("When a person ignores a trade cooldown, the echo message will include their Nintendo Account ID.")]
    public bool EchoNintendoOnlineIDCooldown { get; set; } = true;

    [Category(Monitoring), Description("If not empty, the provided string will be appended to Echo alerts to notify whomever you specify when a user violates trade cooldown. For Discord, use <@userIDnumber> to mention.")]
    public string CooldownAbuseEchoMention { get; set; } = string.Empty;

    [Category(Monitoring), Description("When a person appears with a different Discord/Twitch account in less than this setting's value (minutes), a notification will be sent.")]
    public double TradeAbuseExpiration { get; set; } = 120;

    [Category(Monitoring), Description("When a person using multiple Discord/Twitch accounts is detected, the echo message will include their Nintendo Account ID.")]
    public bool EchoNintendoOnlineIDMulti { get; set; } = true;

    [Category(Monitoring), Description("When a person sending to multiple in-game accounts is detected, the echo message will include their Nintendo Account ID.")]
    public bool EchoNintendoOnlineIDMultiRecipients { get; set; } = true;

    [Category(Monitoring), Description("When a person using multiple Discord/Twitch accounts is detected, this action is taken.")]
    public TradeAbuseAction TradeAbuseAction { get; set; } = TradeAbuseAction.Quit;

    [Category(Monitoring), Description("When a person is blocked in-game for multiple accounts, their online ID is added to BannedIDs.")]
    public bool BanIDWhenBlockingUser { get; set; } = true;

    [Category(Monitoring), Description("If not empty, the provided string will be appended to Echo alerts to notify whomever you specify when a user is found using multiple accounts. For Discord, use <@userIDnumber> to mention.")]
    public string MultiAbuseEchoMention { get; set; } = string.Empty;

    [Category(Monitoring), Description("If not empty, the provided string will be appended to Echo alerts to notify whomever you specify when a user is found sending to multiple players in-game. For Discord, use <@userIDnumber> to mention.")]
    public string MultiRecipientEchoMention { get; set; } = string.Empty;

    [Category(Monitoring), Description("Banned online IDs that will trigger trade exit or in-game block.")]
    public RemoteControlAccessList BannedIDs { get; set; } = new();

    [Category(Monitoring), Description("When a person is encountered with a banned ID, block them in-game before quitting the trade.")]
    public bool BlockDetectedBannedUser { get; set; } = true;

    [Category(Monitoring), Description("If not empty, the provided string will be appended to Echo alerts to notify whomever you specify when a user matches a banned ID. For Discord, use <@userIDnumber> to mention.")]
    public string BannedIDMatchEchoMention { get; set; } = string.Empty;

    [Category(Monitoring), Description("When a person using Ledy nickname swaps is detected of abuse, the echo message will include their Nintendo Account ID.")]
    public bool EchoNintendoOnlineIDLedy { get; set; } = true;

    [Category(Monitoring), Description("If not empty, the provided string will be appended to Echo alerts to notify whomever you specify when a user violates Ledy trade rules. For Discord, use <@userIDnumber> to mention.")]
    public string LedyAbuseEchoMention { get; set; } = string.Empty;
}
