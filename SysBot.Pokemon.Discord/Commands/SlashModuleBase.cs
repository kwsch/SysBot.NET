using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;

namespace SysBot.Pokemon.Discord;

[DefaultMemberPermissions(GuildPermission.SendMessages)]
[RequireBotPermission(GuildPermission.SendMessages)]
public abstract class SlashModuleBase : InteractionModuleBase<SocketInteractionContext>
{
    private static DiscordManager Manager => SysCordSettings.Manager;

    protected bool CheckSudo([NotNullWhen(false)] out string? error)
    {
        if (Manager.Config.AllowGlobalSudo && Manager.CanUseSudo(Context.User.Id))
        {
            error = null;
            return true;
        }

        if (Context.User is not SocketGuildUser guildUser)
        {
            error = "You must be in a guild to run this command.";
            return false;
        }

        if (Manager.CanUseSudo(guildUser.Roles.Select(z => z.Name)))
        {
            error = null;
            return true;
        }

        error = "You are not permitted to run this command.";
        return false;
    }

    protected bool CheckRoleAccess(PokeRoutineType type, [NotNullWhen(false)] out string? error)
    {
        if (Context.User is not SocketGuildUser guildUser)
        {
            error = "You must be in a guild to run this command.";
            return false;
        }

        if (Manager.GetHasRoleAccess(type, guildUser.Roles.Select(z => z.Name)))
        {
            error = null;
            return true;
        }

        error = "You do not have the required role to run this command.";
        return false;
    }

    protected bool CheckQueueAccess(PokeRoutineType type, [NotNullWhen(false)] out string? error)
    {
        if (Manager.Config.AllowGlobalSudo && Manager.CanUseSudo(Context.User.Id))
        {
            error = null;
            return true;
        }

        if (Context.User is not SocketGuildUser guildUser)
        {
            error = "You must be in a guild to run this command.";
            return false;
        }

        var roles = guildUser.Roles.Select(z => z.Name).ToArray();
        if (Manager.CanUseSudo(roles))
        {
            error = null;
            return true;
        }

        if (!SysCordSettings.HubConfig.Queues.CanQueue)
        {
            error = "Sorry, I am not currently accepting queue requests!";
            return false;
        }

        if (!Manager.GetHasRoleAccess(type, roles))
        {
            error = "You do not have the required role to run this command.";
            return false;
        }

        error = null;
        return true;
    }

    protected async Task<bool> RequireAsync(bool allowed, string? error)
    {
        if (allowed)
            return true;

        await RespondAsync(error, ephemeral: true).ConfigureAwait(false);
        return false;
    }

    // ReSharper disable once MemberCanBeMadeStatic.Global
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

    protected RemoteControlAccess GetReference(IChannel channel) => GetReference(channel.Id, channel.Name);
    protected RemoteControlAccess GetReference(IUser user) => GetReference(user.Id, user.Username);
    protected RemoteControlAccess GetReference(ulong id, string name = "Manual") => new()
    {
        ID = id,
        Name = name,
        Comment = $"Added by {Context.User.Username} on {DateTime.Now:yyyy.MM.dd-hh:mm:ss}",
    };
}
