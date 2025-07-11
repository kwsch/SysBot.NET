using System;
using System.ComponentModel;
using System.Linq;

namespace SysBot.Pokemon
{
    public class BilibiliSettings
    {
        private const string Startup = nameof(Startup);

        public override string ToString() => "Bilibili Integration Settings";

        // Startup

        [Category(Startup), Description("B站彈幕姬日志目录")]
        public string LogUrl { get; set; } = string.Empty;

        [Category(Startup), Description("直播间ID")]
        public int RoomId { get; set; } = 0;
    }
}
