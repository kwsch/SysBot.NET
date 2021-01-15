using System.Net;

namespace SysBot.Base
{
    /// <summary>
    /// Stored config of a bot
    /// </summary>
    public static class BotConfigUtil
    {
        public static T GetConfig<T>(string ip, int port) where T : IWirelessBotConfig, new() => new()
        {
            IP = IPAddress.Parse(ip).ToString(), // sanitize leading zeroes out for paranoia's sake
            Port = port,
        };
    }
}
