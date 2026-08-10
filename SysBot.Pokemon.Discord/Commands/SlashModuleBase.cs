using System;
using System.Linq;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;

namespace SysBot.Pokemon.Discord;

/// <summary>
/// Base implementation providing
/// </summary>
[DefaultMemberPermissions(GuildPermission.SendMessages)]
[RequireBotPermission(GuildPermission.SendMessages)]
public abstract class SlashModuleBase : InteractionModuleBase<SocketInteractionContext>
{
    private static DiscordManager Manager => SysCordSettings.Manager;

    protected RequestSignificance GetSignificance(SocketUser user)
    {
        // Check user ID.
        var userId = user.Id;
        if (userId == Manager.Owner)
            return RequestSignificance.Owner;
        if (Manager.CanUseSudo(userId))
            return RequestSignificance.Favored;

        // Check roles, might be a special role granted.
        // Stringy names are for user convenience; must trust externally managed guilds the bot is added to (else we should use role IDs).
        return user is SocketGuildUser g
            ? Manager.GetSignificance(g.Roles.Select(z => z.Name))
            : RequestSignificance.None;
    }

    // Used by Sudo commands.
    protected RemoteControlAccess GetReference(IChannel channel) => GetReference(channel.Id, channel.Name);
    protected RemoteControlAccess GetReference(IUser user) => GetReference(user.Id, user.Username);
    protected RemoteControlAccess GetReference(ulong id, string name = "Manual") => new()
    {
        ID = id,
        Name = name,
        Comment = $"Added by {Context.User.Username} on {DateTime.Now:yyyy.MM.dd-hh:mm:ss}",
    };
}
