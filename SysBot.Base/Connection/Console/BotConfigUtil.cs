using System.Net;

namespace SysBot.Base;

/// <summary>
/// Stored config of a bot
/// </summary>
public static class BotConfigUtil
{
    /// <summary>
    /// Parses the input details into a config object.
    /// </summary>
    /// <typeparam name="T">Type of config object that implements <see cref="IWirelessConnectionConfig"/></typeparam>
    /// <param name="ip">IP address string for the connection</param>
    /// <param name="port">Port of the connection</param>
    public static T GetConfig<T>(string ip, int port) where T : IWirelessConnectionConfig, new() => new()
    {
        IP = IPAddress.Parse(ip).ToString(), // sanitize leading zeroes out for paranoia's sake
        Port = port,
    };
}
