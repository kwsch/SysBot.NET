using System.Collections.Generic;
using System.Linq;

namespace SysBot.Pokemon.Discord
{
    public class DiscordManager
    {
        public readonly PokeTradeHubConfig Config;
        public ulong Owner;

        public readonly SensitiveSet<ulong> BlacklistedUsers = new SensitiveSet<ulong>();
        public readonly SensitiveSet<ulong> WhitelistedChannels = new SensitiveSet<ulong>();

        public readonly SensitiveSet<ulong> SudoDiscord = new SensitiveSet<ulong>();
        public readonly SensitiveSet<string> SudoRoles = new SensitiveSet<string>();

        public readonly SensitiveSet<string> RolesClone = new SensitiveSet<string>();
        public readonly SensitiveSet<string> RolesTrade = new SensitiveSet<string>();
        public readonly SensitiveSet<string> RolesDudu = new SensitiveSet<string>();

        public bool CanUseSudo(ulong uid) => SudoDiscord.Contains(uid);
        public bool CanUseSudo(IEnumerable<string> roles) => roles.Any(SudoRoles.Contains);

        public bool CanUseCommandChannel(ulong channel) => WhitelistedChannels.Count == 0 || WhitelistedChannels.Contains(channel);
        public bool CanUseCommandUser(ulong uid) => !BlacklistedUsers.Contains(uid);

        private const string ALLOW_ALL = "@everyone";

        public DiscordManager(PokeTradeHubConfig cfg)
        {
            Config = cfg;
            Read();
        }

        public bool GetHasRoleQueue(string type, IEnumerable<string> roles)
        {
            return type switch
            {
                nameof(RolesClone) => roles.Any(RolesClone.Contains) || RolesClone.Contains(ALLOW_ALL),
                nameof(RolesTrade) => roles.Any(RolesTrade.Contains) || RolesTrade.Contains(ALLOW_ALL),
                nameof(RolesDudu) => roles.Any(RolesDudu.Contains) || RolesDudu.Contains(ALLOW_ALL),
                _ => false
            };
        }

        public void Read()
        {
            var cfg = Config;
            BlacklistedUsers.Read(cfg.DiscordBlackList, ulong.Parse);
            WhitelistedChannels.Read(cfg.DiscordWhiteList, ulong.Parse);

            SudoDiscord.Read(cfg.GlobalSudoList, ulong.Parse);
            SudoRoles.Read(cfg.DiscordRoleSudo, z => z);

            RolesClone.Read(cfg.DiscordRoleCanClone, z => z);
            RolesTrade.Read(cfg.DiscordRoleCanTrade, z => z);
            RolesDudu.Read(cfg.DiscordRoleCanDudu, z => z);
        }

        public void Write()
        {
            Config.DiscordBlackList = BlacklistedUsers.Write();
            Config.DiscordWhiteList = WhitelistedChannels.Write();
            Config.DiscordRoleSudo = SudoRoles.Write();
            Config.GlobalSudoList = SudoDiscord.Write();

            Config.DiscordRoleCanClone = RolesClone.Write();
            Config.DiscordRoleCanTrade = RolesTrade.Write();
            Config.DiscordRoleCanDudu = RolesDudu.Write();
        }
    }
}