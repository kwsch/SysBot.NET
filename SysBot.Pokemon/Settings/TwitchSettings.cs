using System;
using System.ComponentModel;
using System.Linq;

namespace SysBot.Pokemon
{
    public class TwitchSettings
    {
        private const string Startup = nameof(Startup);
        private const string Operation = nameof(Operation);
        public override string ToString() => "Twitch Integration Settings";

        // Startup

        [Category(Startup), Description("Bot Login Token")]
        public string Token { get; set; } = string.Empty;

        [Category(Startup), Description("Bot Username")]
        public string Username { get; set; } = string.Empty;

        [Category(Startup), Description("Channel to Send Messages To")]
        public string Channel { get; set; } = string.Empty;

        [Category(Startup), Description("Bot Command Prefix")]
        public char CommandPrefix { get; set; } = '$';

        [Category(Operation), Description("Message sent when the Barrier is released.")]
        public string MessageStart { get; set; } = string.Empty;

        [Category(Operation), Description("Throttle the bot from sending messages if X messages have been sent in the past Y seconds.")]
        public int ThrottleMessages { get; set; } = 100;

        [Category(Operation), Description("Throttle the bot from sending messages if X messages have been sent in the past Y seconds.")]
        public double ThrottleSeconds { get; set; } = 30;

        // Operation

        [Category(Operation), Description("Sudo Usernames")]
        public string SudoList { get; set; } = string.Empty;

        [Category(Operation), Description("Users with these usernames cannot use the bot.")]
        public string UserBlacklist { get; set; } = string.Empty;

        [Category(Operation), Description("Sub only mode (Restricts the bot to twitch subs only)")]
        public bool SubOnlyBot { get; set; } = false;

        [Category(Operation), Description("When enabled, the bot will process commands sent to the channel.")]
        public bool AllowCommandsViaChannel { get; set; } = true;

        [Category(Operation), Description("When enabled, the bot will allow users to send command via whisper (bypasses slow mode)")]
        public bool AllowCommandsViaWhisper { get; set; }

        public bool IsSudo(string username)
        {
            var sudos = SudoList.Split(new[] { ",", ", ", " " }, StringSplitOptions.RemoveEmptyEntries);
            return sudos.Contains(username);
        }
    }
}
