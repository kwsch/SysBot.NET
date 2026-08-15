using System;
using Discord;
using Discord.Interactions;

namespace SysBot.Pokemon.Discord;

/// <summary>
/// Base implementation providing
/// </summary>
[DefaultMemberPermissions(GuildPermission.SendMessages)]
[RequireBotPermission(GuildPermission.SendMessages)]
public abstract class SlashModuleBase : InteractionModuleBase<SocketInteractionContext>
{
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
