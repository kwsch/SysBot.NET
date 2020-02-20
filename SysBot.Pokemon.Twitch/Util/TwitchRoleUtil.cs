namespace SysBot.Pokemon.Twitch
{
    public static class TwitchRoleUtil
    {
        // Util for checking subscribers/ mods/ sudos etc. Future expandability??
        public static bool IsSudo(string username)
        {
            var cfg = TwitchBot.Info.Hub.Config;
            return cfg.TwitchSudoList.Contains(username);
        }
    }
}
