using System;
using System.Collections.Generic;
using System.Linq;

namespace SysBot.Pokemon.Discord;

public sealed record DiscordManager(DiscordSettings Config)
{
    public ulong Owner { get; internal set; }

    public RemoteControlAccessList BlacklistedUsers => Config.UserBlacklist;
    public RemoteControlAccessList WhitelistedChannels => Config.ChannelWhitelist;

    public RemoteControlAccessList SudoDiscord => Config.GlobalSudoList;
    public RemoteControlAccessList SudoRoles => Config.RoleSudo;
    public RemoteControlAccessList FavoredRoles => Config.RoleFavored;

    public RemoteControlAccessList RolesClone => Config.RoleCanClone;
    public RemoteControlAccessList RolesTrade => Config.RoleCanTrade;
    public RemoteControlAccessList RolesSeed => Config.RoleCanSeedCheck;
    public RemoteControlAccessList RolesDump => Config.RoleCanDump;
    public RemoteControlAccessList RolesRemoteControl => Config.RoleRemoteControl;

    public bool CanUseSudo(ulong uid) => uid == Owner || SudoDiscord.Contains(uid);
    public bool CanUseSudo(IEnumerable<string> roles) => roles.Any(SudoRoles.Contains);

    public bool CanUseCommandChannel(ulong channel) => (WhitelistedChannels.List.Count == 0 && WhitelistedChannels.AllowIfEmpty) || WhitelistedChannels.Contains(channel);
    public bool CanUseCommandUser(ulong uid) => uid == Owner || !BlacklistedUsers.Contains(uid);

    public RequestSignificance GetSignificance(IEnumerable<string> roles)
    {
        var result = RequestSignificance.None;
        foreach (var r in roles)
        {
            if (SudoRoles.Contains(r))
                result = RequestSignificance.Favored;
            if (FavoredRoles.Contains(r))
                result = RequestSignificance.Favored;
        }
        return result;
    }

    public bool GetHasRoleAccess(PokeRoutineType type, IEnumerable<string> roles)
    {
        var set = GetSet(type);
        return set is { AllowIfEmpty: true, List.Count: 0 } || roles.Any(set.Contains);
    }

    private RemoteControlAccessList GetSet(PokeRoutineType type) => type switch
    {
        PokeRoutineType.Clone => RolesClone,
        PokeRoutineType.LinkTrade => RolesTrade,
        PokeRoutineType.SeedCheck => RolesSeed,
        PokeRoutineType.Dump => RolesDump,
        PokeRoutineType.RemoteControl => RolesRemoteControl,
        _ => throw new ArgumentOutOfRangeException(nameof(type)),
    };
}
