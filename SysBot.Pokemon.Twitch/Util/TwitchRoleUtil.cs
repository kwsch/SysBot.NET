using System;
using System.Linq;
using PKHeX.Core;

namespace SysBot.Pokemon.Twitch
{
    public static class TwitchRoleUtil
    {
        // Util for checking subscribers/ mods/ sudos etc. Future expandability??
        public static bool IsSudo(this PokeTradeHub<PK8> hub, string username)
        {
            var sudos = hub.Config.TwitchSudoList.Split(new[] { ",", ", ", " " }, StringSplitOptions.RemoveEmptyEntries);
            return sudos.Contains(username);
        }
    }
}
