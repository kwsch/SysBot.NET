using System.ComponentModel;

namespace SysBot.Pokemon
{
    public class DiscordSettings
    {
        private const string Startup = nameof(Startup);
        private const string Operation = nameof(Operation);
        private const string Channels = nameof(Channels);
        private const string Roles = nameof(Roles);
        private const string Users = nameof(Users);
        public override string ToString() => "Discord Integration Settings";

        // Startup

        [Category(Startup), Description("Bot login token.")]
        public string Token { get; set; } = string.Empty;

        [Category(Startup), Description("Bot command prefix.")]
        public string CommandPrefix { get; set; } = "$";

        [Category(Startup), Description("List of modules that will not be loaded when the bot is started (comma separated).")]
        public string ModuleBlacklist { get; set; } = string.Empty;

        [Category(Startup), Description("Toggle to handle commands asynchronously or synchronously.")]
        public bool AsyncCommands { get; set; }

        [Category(Startup), Description("Custom Status for playing a game.")]
        public string BotGameStatus { get; set; } = "SysBot.NET: Pokémon";

        [Category(Startup), Description("Indicates the Discord presence status color only considering bots that are Trade-type.")]
        public bool BotColorStatusTradeOnly { get; set; } = true;

        [Category(Operation), Description("Custom message the bot will reply with when a user says hello to it. Use string formatting to mention the user in the reply.")]
        public string HelloResponse { get; set; } = "Hi {0}!";

        // Whitelists

        [Category(Roles), Description("Users with this role are allowed to enter the Trade queue.")]
        public RemoteControlAccessList RoleCanTrade { get; set; } = new() { AllowIfEmpty = false };

        [Category(Roles), Description("Users with this role are allowed to enter the Seed Check queue.")]
        public RemoteControlAccessList RoleCanSeedCheck { get; set; } = new() { AllowIfEmpty = false };

        [Category(Roles), Description("Users with this role are allowed to enter the Clone queue.")]
        public RemoteControlAccessList RoleCanClone { get; set; } = new() { AllowIfEmpty = false };

        [Category(Roles), Description("Users with this role are allowed to enter the Dump queue.")]
        public RemoteControlAccessList RoleCanDump { get; set; } = new() { AllowIfEmpty = false };

        [Category(Roles), Description("Users with this role are allowed to remotely control the console (if running as Remote Control Bot.")]
        public RemoteControlAccessList RoleRemoteControl { get; set; } = new() { AllowIfEmpty = false };

        [Category(Roles), Description("Users with this role are allowed to bypass command restrictions.")]
        public RemoteControlAccessList RoleSudo { get; set; } = new() { AllowIfEmpty = false };

        // Operation

        [Category(Roles), Description("Users with this role are allowed to join the queue with a better position.")]
        public RemoteControlAccessList RoleFavored { get; set; } = new() { AllowIfEmpty = false };

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

        [Category(Channels), Description("Echo channels that will log special messages.")]
        public RemoteControlAccessList EchoChannels { get; set; } = new();

        [Category(Operation), Description("Returns PKMs of Pokémon shown in the trade to the user.")]
        public bool ReturnPKMs { get; set; } = true;

        [Category(Operation), Description("Replies to users if they are not allowed to use a given command in the channel. When false, the bot will silently ignore them instead.")]
        public bool ReplyCannotUseCommandInChannel { get; set; } = true;
    }
}