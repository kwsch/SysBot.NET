namespace SysBot.Base
{
    /// <summary>
    /// Stored config of a bot
    /// </summary>
    public class SwitchBotConfig
    {
        public readonly string IP;
        public readonly int Port;
        public readonly string[] Lines;

        public SwitchBotConfig(string[] lines)
        {
            IP = lines[0];
            Port = int.Parse(lines[1]);
            Lines = lines;
        }
    }
}