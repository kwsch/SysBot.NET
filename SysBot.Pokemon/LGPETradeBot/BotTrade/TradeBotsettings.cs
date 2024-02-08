using System.ComponentModel;
using System;
using System.Collections.Generic;


namespace SysBot.Pokemon.LGPETradeBot.BotTrade
{
    public class TradebotSettings
    {
        private const string TradeBot = nameof(TradeBot);
        public override string ToString() => "Trade Bot Settings";
        public static Action<List<LetsGoTrades.pictocodes>>? CreateSpriteFile { get; set; }

        [Category(TradeBot), Description("The channel(s) the bot will be accepting commands in separated by a comma, no spaces at all.")]
        public string tradebotchannel { get; set; } = string.Empty;

        [Category(TradeBot), Description("Turn this setting on to have the bot update discord channels for when it is online/offline when you press start or stop")]

        public bool channelchanger { get; set; } = false;

        [Category(TradeBot), Description("The name of your discord trade bot channel")]
        public string channelname { get; set; } = string.Empty;

        [Category(TradeBot), Description("MGDB folder path")]
        public string mgdbpath { get; set; } = string.Empty;
        [Category(TradeBot), Description("Turn on to Distribute")]
        public bool distribution { get; set; } = false;

        public static void generatebotsprites(List<LetsGoTrades.pictocodes> code)
        {
            var func = CreateSpriteFile;
            if (func == null)
                return;
            func.Invoke(code);
        }
    }
}
