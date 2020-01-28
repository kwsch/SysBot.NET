using SysBot.Base;

namespace SysBot.Pokemon
{
    public class SurpriseTradeBotConfig : SwitchBotConfig
    {
        public readonly string? DistributeFolder;
        public readonly string? DumpFolder;

        public SurpriseTradeBotConfig(string[] lines) : base(lines)
        {
            if (lines.Length > 2)
                DistributeFolder = lines[2];
            if (lines.Length > 3)
                DumpFolder = lines[3];
        }
    }
}