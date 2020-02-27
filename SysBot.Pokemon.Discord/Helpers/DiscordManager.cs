using System.Collections.Generic;
using System.Linq;

namespace SysBot.Pokemon.Discord
{
    public class DiscordManager
    {
        private readonly PokeTradeHubConfig Config;

        public readonly SensitiveSet<ulong> BlacklistedUsers = new SensitiveSet<ulong>();
        public readonly SensitiveSet<ulong> WhitelistedChannels = new SensitiveSet<ulong>();

        public readonly SensitiveSet<ulong> SudoDiscord = new SensitiveSet<ulong>();
        public readonly SensitiveSet<string> SudoRoles = new SensitiveSet<string>();

        public readonly SensitiveSet<string> RolesClone = new SensitiveSet<string>();
        public readonly SensitiveSet<string> RolesTrade = new SensitiveSet<string>();
        public readonly SensitiveSet<string> RolesDudu = new SensitiveSet<string>();

        public DiscordManager(PokeTradeHubConfig cfg) => Config = cfg;

        public bool CanUseSudo(ulong uid) => SudoDiscord.Contains(uid);
        public bool CanUseSudo(IEnumerable<string> roles) => roles.Any(SudoRoles.Contains);

        public bool CanUseCommandChannel(ulong channel) => WhitelistedChannels.Count == 0 || WhitelistedChannels.Contains(channel);
        public bool CanUseCommandUser(ulong uid) => !BlacklistedUsers.Contains(uid);

        public void Read()
        {
            var cfg = Config;
            BlacklistedUsers.Read(cfg.DiscordBlackList, ulong.Parse);
            //WhitelistedChannels.Read(cfg);

            SudoDiscord.Read(cfg.GlobalSudoList, ulong.Parse);
            SudoRoles.Read(cfg.DiscordRoleSudo, z => z);

            RolesClone.Read(cfg.DiscordRoleCanClone, z => z);
            RolesTrade.Read(cfg.DiscordRoleCanTrade, z => z);
            RolesDudu.Read(cfg.DiscordRoleCanDudu, z => z);
        }

        public void Write()
        {
            Config.DiscordBlackList = BlacklistedUsers.Write();
            Config.DiscordRoleSudo = SudoRoles.Write();
            Config.GlobalSudoList = SudoDiscord.Write();

            Config.DiscordRoleCanClone = RolesClone.Write();
            Config.DiscordRoleCanTrade = RolesTrade.Write();
            Config.DiscordRoleCanDudu = RolesDudu.Write();
        }
    }
}