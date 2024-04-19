using Discord.Commands;
using Discord.WebSocket;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace SysBot.Pokemon.Discord;

/// <summary>
/// Requires an assigned role in order to accept commands. Can be used by sudo users if satisfied.
/// </summary>
public sealed class RequireRoleAccessAttribute(string RoleName) : PreconditionAttribute
{
    // Create a field to store the specified name

    // Create a constructor so the name can be specified

    public override Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context, CommandInfo command, IServiceProvider services)
    {
        var mgr = SysCordSettings.Manager;
        if (mgr.Config.AllowGlobalSudo && mgr.CanUseSudo(context.User.Id))
            return Task.FromResult(PreconditionResult.FromSuccess());

        // Check if this user is a Guild User, which is the only context where roles exist
        if (context.User is not SocketGuildUser gUser)
            return Task.FromResult(PreconditionResult.FromError("Sie müssen die Nachricht von einer Gilde aus senden, um diesen Befehl auszuführen."));

        var roles = gUser.Roles;
        if (mgr.CanUseSudo(roles.Select(z => z.Name)))
            return Task.FromResult(PreconditionResult.FromSuccess());

        if (!mgr.GetHasRoleAccess(RoleName, roles.Select(z => z.Name)))
            return Task.FromResult(PreconditionResult.FromError("Sie haben nicht die erforderliche Rolle, um diesen Befehl auszuführen."));

        return Task.FromResult(PreconditionResult.FromSuccess());
    }
}
