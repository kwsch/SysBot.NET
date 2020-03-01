using System.ComponentModel;

namespace SysBot.Pokemon
{
    public class DiscordSettings
    {
        private const string Startup = nameof(Startup);
        private const string Operation = nameof(Operation);
        private const string Whitelists = nameof(Whitelists);
        private const string DefaultDisable = "DISABLE";
        public override string ToString() => "Discord Integration Settings";

        // Startup

        [Category(Startup), Description("Bot Login Token")]
        public string Token { get; set; } = string.Empty;

        [Category(Startup), Description("Bot Command Prefix")]
        public string CommandPrefix { get; set; } = "$";

        [Category(Startup), Description("List of Modules that will not be loaded when the Bot is started (comma separated).")]
        public string ModuleBlacklist { get; set; } = string.Empty;

        [Category(Startup), Description("Custom Status for playing a game.")]
        public string BotGameStatus { get; set; } = "SysBot.NET: Pokémon";

        // Whitelists

        [Category(Whitelists), Description("Users with this role are allowed to enter the Trade queue.")]
        public string RoleCanTrade { get; set; } = DefaultDisable;

        [Category(Whitelists), Description("Users with this role are allowed to enter the Dudu queue.")]
        public string RoleCanDudu { get; set; } = DefaultDisable;

        [Category(Whitelists), Description("Users with this role are allowed to enter the Clone queue.")]
        public string RoleCanClone { get; set; } = DefaultDisable;

        [Category(Whitelists), Description("Users with this role are allowed to enter the Dump queue.")]
        public string RoleCanDump { get; set; } = DefaultDisable;

        [Category(Whitelists), Description("Users with this role are allowed to bypass command restrictions.")]
        public string RoleSudo { get; set; } = DefaultDisable;

        // Operation

        [Category(Operation), Description("Users with these user IDs cannot use the bot.")]
        public string UserBlacklist { get; set; } = string.Empty;

        [Category(Operation), Description("Channels with these IDs are the only channels where the bot acknowledges commands.")]
        public string ChannelWhitelist { get; set; } = string.Empty;

        [Category(Operation), Description("Comma separated Discord user IDs that will have sudo access to the Bot Hub.")]
        public string GlobalSudoList { get; set; } = string.Empty;

        [Category(Operation), Description("Disabling this will remove global sudo support.")]
        public bool AllowGlobalSudo { get; set; } = true;

        [Category(Operation), Description("Comma separated Logger channel IDs that will persistently log bot data.")]
        public string LoggingChannels { get; set; } = string.Empty;
    }
}