using System;
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
        public readonly SensitiveSet<string> RolesSeed = new SensitiveSet<string>();
        public readonly SensitiveSet<string> RolesDump = new SensitiveSet<string>();

        public bool CanUseSudo(ulong uid) => SudoDiscord.Contains(uid);
        public bool CanUseSudo(IEnumerable<string> roles) => roles.Any(SudoRoles.Contains);

        public bool CanUseCommandChannel(ulong channel) => WhitelistedChannels.Count == 0 || WhitelistedChannels.Contains(channel);
        public bool CanUseCommandUser(ulong uid) => !BlacklistedUsers.Contains(uid);

        public DiscordManager(PokeTradeHubConfig cfg)
        {
            Config = cfg;
            Read();
        }

        public bool GetHasRoleQueue(string type, IEnumerable<string> roles)
        {
            var set = GetSet(type);
            return set.Count == 0 || roles.Any(set.Contains);
        }

        private SensitiveSet<string> GetSet(string type)
        {
            return type switch
            {
                nameof(RolesClone) => RolesClone,
                nameof(RolesTrade) => RolesTrade,
                nameof(RolesSeed) => RolesSeed,
                nameof(RolesDump) => RolesDump,
                _ => throw new ArgumentOutOfRangeException(nameof(type)),
            };
        }

        public void Read()
        {
            var cfg = Config;
            BlacklistedUsers.Read(cfg.Discord.UserBlacklist, ulong.Parse);
            WhitelistedChannels.Read(cfg.Discord.ChannelWhitelist, ulong.Parse);

            SudoDiscord.Read(cfg.Discord.GlobalSudoList, ulong.Parse);
            SudoRoles.Read(cfg.Discord.RoleSudo, z => z);

            RolesClone.Read(cfg.Discord.RoleCanClone, z => z);
            RolesTrade.Read(cfg.Discord.RoleCanTrade, z => z);
            RolesSeed.Read(cfg.Discord.RoleCanSeedCheck, z => z);
            RolesDump.Read(cfg.Discord.RoleCanDump, z => z);
        }

        public void Write()
        {
            Config.Discord.UserBlacklist = BlacklistedUsers.Write();
            Config.Discord.ChannelWhitelist = WhitelistedChannels.Write();
            Config.Discord.RoleSudo = SudoRoles.Write();
            Config.Discord.GlobalSudoList = SudoDiscord.Write();

            Config.Discord.RoleCanClone = RolesClone.Write();
            Config.Discord.RoleCanTrade = RolesTrade.Write();
            Config.Discord.RoleCanSeedCheck = RolesSeed.Write();
            Config.Discord.RoleCanDump = RolesDump.Write();
        }
    }
}