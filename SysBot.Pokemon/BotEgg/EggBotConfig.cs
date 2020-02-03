using SysBot.Base;

namespace SysBot.Pokemon
{
    public class EggBotConfig : SwitchBotConfig
    {
        public readonly string? DumpFolder;

        public EggBotConfig(string[] lines) : base(lines)
        {
            if (lines.Length > 2)
                DumpFolder = lines[2];
        }
    }
}