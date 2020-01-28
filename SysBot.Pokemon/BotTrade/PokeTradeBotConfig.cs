using SysBot.Base;

namespace SysBot.Pokemon
{
    public class PokeTradeBotConfig : SwitchBotConfig
    {
        public readonly string? DumpFolder;

        public PokeTradeBotConfig(string[] lines) : base(lines)
        {
            if (lines.Length > 2)
                DumpFolder = lines[2];
        }
    }
}
