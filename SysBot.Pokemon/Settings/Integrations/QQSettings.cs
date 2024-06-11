using System.ComponentModel;

namespace SysBot.Pokemon
{
    public class QQSettings
    {
        private const string Messages = nameof(Messages);

        private const string Operation = nameof(Operation);

        private const string Startup = nameof(Startup);

        [Category(Startup), Description("Mirai Bot Address")]
        public string Address { get; set; } = string.Empty;

        [Category(Startup), Description("Message to test Bot alive")]
        public string AliveMsg { get; set; } = "hello";

        [Category(Startup), Description("QQ Group to Send Messages To")]
        public string GroupId { get; set; } = string.Empty;

        [Category(Operation), Description("Message sent when the Barrier is released.")]
        public string MessageStart { get; set; } = string.Empty;

        [Category(Startup), Description("QQ number of your Bot")]
        public string QQ { get; set; } = string.Empty;

        // Startup
        [Category(Startup), Description("Mirai Bot VerifyKey")]
        public string VerifyKey { get; set; } = string.Empty;

        public override string ToString() => "QQ Integration Settings";
    }
}
