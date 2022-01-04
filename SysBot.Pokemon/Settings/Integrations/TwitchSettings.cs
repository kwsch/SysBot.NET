﻿using System;
using System.ComponentModel;
using System.Linq;

namespace SysBot.Pokemon
{
    public class TwitchSettings
    {
        private const string Startup = nameof(Startup);
        private const string Operation = nameof(Operation);
        private const string Messages = nameof(Messages);
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

        // Messaging

        [Category(Operation), Description("Throttle the bot from sending messages if X messages have been sent in the past Y seconds.")]
        public int ThrottleMessages { get; set; } = 100;

        [Category(Operation), Description("Throttle the bot from sending messages if X messages have been sent in the past Y seconds.")]
        public double ThrottleSeconds { get; set; } = 30;

        [Category(Operation), Description("Throttle the bot from sending whispers if X messages have been sent in the past Y seconds.")]
        public int ThrottleWhispers { get; set; } = 100;

        [Category(Operation), Description("Throttle the bot from sending whispers if X messages have been sent in the past Y seconds.")]
        public double ThrottleWhispersSeconds { get; set; } = 60;

        // Operation
        [Category(Operation), Description("Subscriber only Trades")]
        public bool SubTradesOnly { get; set; } = false;

        [Category(Operation), Description("Allow Egg Requests when Nicknaming the Pokemon Egg")]
        public bool CreateEggsWithNickname { get; set; } = true;

        [Category(Operation), Description("Subscriber only Eggs")]
        public bool EggsSubsOnly { get; set; } = false;

        [Category(Operation), Description("Nickname all Pokemon from Non Subscriber Trades")]
        public bool AutoNicknameNonSubs { get; set; } = true;

        [Category(Operation), Description("Nickname all Pokemon from Non Subscriber Trades")]
        public string AutoNickname { get; set; } = "PlsSubscribe";

        [Category(Operation), Description("Specific OT for all Pokemon from Non Subscriber Trades")]
        public bool AutoOTNonSubs { get; set; } = true;

        [Category(Operation), Description("Specific OT for all Pokemon from Non Subscriber Trades")]
        public string AutoOT { get; set; } = "PlsSubscribe";

        [Category(Operation), Description("Allow Held Items for Subscriber only")]
        public bool HeldItemsSubsOnly { get; set; } = false;

        [Category(Operation), Description("Sudo Usernames")]
        public string SudoList { get; set; } = string.Empty;

        [Category(Operation), Description("Users with these usernames cannot use the bot.")]
        public string UserBlacklist { get; set; } = string.Empty;

        [Category(Operation), Description("When enabled, the bot will process commands sent to the channel.")]
        public bool AllowCommandsViaChannel { get; set; } = true;

        [Category(Operation), Description("When enabled, the bot will allow users to send command via whisper (bypasses slow mode)")]
        public bool AllowCommandsViaWhisper { get; set; }

        // Message Destinations

        [Category(Messages), Description("Determines where generic notifications are sent.")]
        public TwitchMessageDestination NotifyDestination { get; set; }

        [Category(Messages), Description("Determines where TradeStart notifications are sent.")]
        public TwitchMessageDestination TradeStartDestination { get; set; } = TwitchMessageDestination.Channel;

        [Category(Messages), Description("Determines where TradeSearch notifications are sent.")]
        public TwitchMessageDestination TradeSearchDestination { get; set; }

        [Category(Messages), Description("Determines where TradeFinish notifications are sent.")]
        public TwitchMessageDestination TradeFinishDestination { get; set; }

        [Category(Messages), Description("Determines where TradeCanceled notifications are sent.")]
        public TwitchMessageDestination TradeCanceledDestination { get; set; } = TwitchMessageDestination.Channel;

        [Category(Messages), Description("Toggles whether Distribution trades count down before starting.")]
        public bool DistributionCountDown { get; set; } = true;

        public bool IsSudo(string username)
        {
            var sudos = SudoList.Split(new[] { ",", ", ", " " }, StringSplitOptions.RemoveEmptyEntries);
            return sudos.Contains(username);
        }
    }

    public enum TwitchMessageDestination
    {
        Disabled,
        Channel,
        Whisper,
    }
}
