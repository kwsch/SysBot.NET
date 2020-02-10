namespace SysBot.Base
{
    /// <summary>
    /// Stored config of a bot
    /// </summary>
    public abstract class SwitchBotConfig
    {
        public string IP { get; set; } = string.Empty;
        public int Port { get; set; } = 6000;

        public static T GetConfig<T>(string[] lines) where T : SwitchBotConfig, new()
        {
            return GetConfig<T>(lines[0], int.Parse(lines[1]));
        }

        public static T GetConfig<T>(string ip, int port) where T : SwitchBotConfig, new()
        {
            return new T
            {
                IP = ip,
                Port = port,
            };
        }
    }
}