using PKHeX.Core;

namespace SysBot.Pokemon.Twitch
{
    public static class TwitchRoleUtil
    {
        // Util for checking subscribers/ mods/ sudos etc. Future expandability??
        public static bool IsSudo(this PokeTradeHub<PK8> hub, string username)
        {
            var cfg = hub.Config;
            return cfg.TwitchSudoList.Contains(username);
        }
    }
}
