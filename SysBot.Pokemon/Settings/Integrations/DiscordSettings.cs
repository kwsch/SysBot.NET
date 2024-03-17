using System.ComponentModel;
using System;
using static SysBot.Pokemon.TradeSettings;

namespace SysBot.Pokemon;

public class DiscordSettings
{
    private const string Startup = nameof(Startup);
    private const string Operation = nameof(Operation);
    private const string Channels = nameof(Channels);
    private const string Roles = nameof(Roles);
    private const string Users = nameof(Users);
    private const string Servers = nameof(Servers);

    public override string ToString() => "Discord Integration Settings";

    // Startup

    [Category(Startup), Description("Bot login token.")]
    public string Token { get; set; } = string.Empty;

    [Category(Startup), Description("Bot command prefix.")]
    public string CommandPrefix { get; set; } = "$";

    [Category(Startup), Description("Will send a status Embed for Online/Offline to all Whitelisted Channels.")]
    public bool BotEmbedStatus { get; set; } = true;

    [Category(Startup), Description("List of modules that will not be loaded when the bot is started (comma separated).")]
    public string ModuleBlacklist { get; set; } = string.Empty;

    [Category(Startup), Description("Toggle to handle commands asynchronously or synchronously.")]
    public bool AsyncCommands { get; set; }

    [Category(Startup), Description("Custom Status for playing a game.")]
    public string BotGameStatus { get; set; } = "SysBot.NET: Pokémon";

    [Category(Startup), Description("Indicates the Discord presence status color only considering bots that are Trade-type.")]
    public bool BotColorStatusTradeOnly { get; set; } = true;

    [Category(Operation), Description("Additional text to add to the beginning of the embed description.")]
    public string[] AdditionalEmbedText { get; set; } = Array.Empty<string>();

    [Category(Operation), Description("Custom message the bot will reply with when a user says hello to it. Use string formatting to mention the user in the reply.")]
    public string HelloResponse { get; set; } = "Hi {0}!";

    // Whitelists

    [Category(Roles), Description("Users with this role are allowed to enter the Trade queue.")]
    public RemoteControlAccessList RoleCanTrade { get; set; } = new() { AllowIfEmpty = false };

    [Category(Roles), Description("Users with this role are allowed to enter the Seed Check/Special Request queue.")]
    public RemoteControlAccessList RoleCanSeedCheckorSpecialRequest { get; set; } = new() { AllowIfEmpty = false };

    [Category(Roles), Description("Users with this role are allowed to enter the Clone queue.")]
    public RemoteControlAccessList RoleCanClone { get; set; } = new() { AllowIfEmpty = false };

    [Category(Roles), Description("Users with this role are allowed to enter the Dump queue.")]
    public RemoteControlAccessList RoleCanDump { get; set; } = new() { AllowIfEmpty = false };

    [Category(Roles), Description("Users with this role are allowed to enter the FixOT queue.")]
    public RemoteControlAccessList RoleCanFixOT { get; set; } = new() { AllowIfEmpty = false };

    [Category(Roles), Description("Users with this role are allowed to remotely control the console (if running as Remote Control Bot.")]
    public RemoteControlAccessList RoleRemoteControl { get; set; } = new() { AllowIfEmpty = false };

    [Category(Roles), Description("Users with this role are allowed to bypass command restrictions.")]
    public RemoteControlAccessList RoleSudo { get; set; } = new() { AllowIfEmpty = false };

    // Operation

    [Category(Roles), Description("Users with this role are allowed to join the queue with a better position.")]
    public RemoteControlAccessList RoleFavored { get; set; } = new() { AllowIfEmpty = false };

    [Category(Servers), Description("Servers with these IDs will not be able to use the bot, and it will leave the server.")]
    public RemoteControlAccessList ServerBlacklist { get; set; } = new() { AllowIfEmpty = false };

    [Category(Users), Description("Users with these user IDs cannot use the bot.")]
    public RemoteControlAccessList UserBlacklist { get; set; } = new();

    [Category(Channels), Description("Channels with these IDs are the only channels where the bot acknowledges commands.")]
    public RemoteControlAccessList ChannelWhitelist { get; set; } = new();

    [Category(Users), Description("Comma separated Discord user IDs that will have sudo access to the Bot Hub.")]
    public RemoteControlAccessList GlobalSudoList { get; set; } = new();

    [Category(Users), Description("Disabling this will remove global sudo support.")]
    public bool AllowGlobalSudo { get; set; } = true;

    [Category(Channels), Description("Channel IDs that will echo the log bot data.")]
    public RemoteControlAccessList LoggingChannels { get; set; } = new();

    [Category(Channels), Description("Logger channels that will log trade start messages.")]
    public RemoteControlAccessList TradeStartingChannels { get; set; } = new();

    [Category(Channels), Description("Channels that will log special messages, like announcements.")]
    public RemoteControlAccessList AnnouncementChannels { get; set; } = new();

    public AnnouncementSettingsCategory AnnouncementSettings { get; set; } = new();

    [Category(Operation), TypeConverter(typeof(CategoryConverter<AnnouncementSettingsCategory>))]
    public class AnnouncementSettingsCategory
    {
        public override string ToString() => "Announcement Settings";
        [Category("Embed Settings"), Description("Thumbnail option for announcements.")]
        public ThumbnailOption AnnouncementThumbnailOption { get; set; } = ThumbnailOption.Gengar;

        [Category("Embed Settings"), Description("Custom thumbnail URL for announcements.")]
        public string CustomAnnouncementThumbnailUrl { get; set; } = string.Empty;
        public EmbedColorOption AnnouncementEmbedColor { get; set; } = EmbedColorOption.Purple;
        [Category("Embed Settings"), Description("Enable random thumbnail selection for announcements.")]
        public bool RandomAnnouncementThumbnail { get; set; } = false;

        [Category("Embed Settings"), Description("Enable random color selection for announcements.")]
        public bool RandomAnnouncementColor { get; set; } = false;
    }

    [Category(Operation), Description("Returns PKMs of Pokémon shown in the trade to the user.")]
    public bool ReturnPKMs { get; set; } = true;

    [Category(Operation), Description("Replies to users if they are not allowed to use a given command in the channel. When false, the bot will silently ignore them instead.")]
    public bool ReplyCannotUseCommandInChannel { get; set; } = true;

    [Category(Operation), Description("Bot listens to channel messages to reply with a ShowdownSet whenever a PKM file is attached (not with a command).")]
    public bool ConvertPKMToShowdownSet { get; set; } = true;

    [Category(Operation), Description("Bot can reply with a ShowdownSet in Any channel the bot can see, instead of only channels the bot has been whitelisted to run in. Only make this true if you want the bot to serve more utility in non-bot channels.")]
    public bool ConvertPKMReplyAnyChannel { get; set; }
    public enum EmbedColorOption
    {
        Blue,
        Green,
        Red,
        Gold,
        Purple,
        Teal,
        Orange,
        Magenta,
        LightGrey,
        DarkGrey
    }

    public enum ThumbnailOption
    {
        Gengar,
        Pikachu,
        Umbreon,
        Sylveon,
        Charmander,
        Jigglypuff,
        Flareon,
        Custom
    }
}
