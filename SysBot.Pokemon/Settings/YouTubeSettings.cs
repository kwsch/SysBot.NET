using System;
using System.ComponentModel;
using System.Linq;

namespace SysBot.Pokemon
{
    public class YouTubeSettings
    {
        private const string Startup = nameof(Startup);
        private const string Operation = nameof(Operation);
        private const string Messages = nameof(Messages);
        public override string ToString() => "YouTube Integration Settings";

        // Startup

        [Category(Startup), Description("Bot ClientID")]
        public string ClientID { get; set; } = string.Empty;

        [Category(Startup), Description("Bot Client Secret")]
        public string ClientSecret { get; set; } = string.Empty;

        [Category(Startup), Description("ChannelID to Send Messages To")]
        public string ChannelID { get; set; } = string.Empty;

        [Category(Startup), Description("Bot Command Prefix")]
        public char CommandPrefix { get; set; } = '$';

        [Category(Operation), Description("Message sent when the Barrier is released.")]
        public string MessageStart { get; set; } = string.Empty;

        // Operation

        [Category(Operation), Description("Sudo Usernames")]
        public string SudoList { get; set; } = string.Empty;

        [Category(Operation), Description("Users with these usernames cannot use the bot.")]
        public string UserBlacklist { get; set; } = string.Empty;

        public bool IsSudo(string username)
        {
            var sudos = SudoList.Split(new[] { ",", ", ", " " }, StringSplitOptions.RemoveEmptyEntries);
            return sudos.Contains(username);
        }
    }

    public enum YouTubeMessageDestination
    {
        Disabled,
        Channel,
    }
}
