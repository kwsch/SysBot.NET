using System;
using System.Net;
using static SysBot.Base.SwitchProtocol;

namespace SysBot.Base
{
    public record SwitchConnectionConfig : ISwitchBotConfig, IWirelessBotConfig
    {
        public SwitchProtocol Protocol { get; set; }
        public string IP { get; set; } = string.Empty;
        public int Port { get; set; } = 6000;

        public bool UseCRLF => Protocol is WiFi;

        public bool IsValid() => Protocol switch
        {
            WiFi => IPAddress.TryParse(IP, out _),
            USB => Port < ushort.MaxValue,
            _ => false,
        };

        public bool Matches(string magic) => Protocol switch
        {
            WiFi => IPAddress.TryParse(magic, out var val) && val.ToString() == IP,
            USB => magic == Port.ToString(),
            _ => false,
        };

        public override string ToString() => Protocol switch
        {
            WiFi => IP,
            USB => Port.ToString(),
            _ => throw new ArgumentOutOfRangeException(nameof(SwitchProtocol)),
        };

        public ISwitchConnectionAsync CreateAsynchronous() => Protocol switch
        {
            WiFi => new SwitchSocketAsync(this),
            USB => new SwitchUSBAsync(Port),
            _ => throw new ArgumentOutOfRangeException(nameof(SwitchProtocol)),
        };

        public ISwitchConnectionSync CreateSync() => Protocol switch
        {
            WiFi => new SwitchSocketSync(this),
            USB => new SwitchUSBSync(Port),
            _ => throw new ArgumentOutOfRangeException(nameof(SwitchProtocol)),
        };
    }

    public enum SwitchProtocol
    {
        WiFi,
        USB,
    }
}
