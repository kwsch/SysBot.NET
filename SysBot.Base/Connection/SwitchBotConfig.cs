using System.Net;

namespace SysBot.Base
{
    /// <summary>
    /// Stored config of a bot
    /// </summary>
    public abstract class SwitchBotConfig
    {
        public string IP { get; set; } = string.Empty;
        public int Port { get; set; } = 6000;

        public bool IsValidIP() => IPAddress.TryParse(IP, out _);
        public IPAddress GetAddress() => IPAddress.Parse(IP);

        public static T GetConfig<T>(string[] lines) where T : SwitchBotConfig, new()
        {
            return GetConfig<T>(lines[0], int.Parse(lines[1]));
        }

        public static T GetConfig<T>(string ip, int port) where T : SwitchBotConfig, new()
        {
            var cfg = new T
            {
                IP = ip,
                Port = port,
            };
            cfg.IP = cfg.GetAddress().ToString(); // sanitize leading zeroes out for paranoia's sake
            return cfg;
        }
    }
}