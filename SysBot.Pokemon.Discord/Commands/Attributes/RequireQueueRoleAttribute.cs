using System;
using System.Linq;
using System.Threading.Tasks;
using Discord.Commands;
using Discord.WebSocket;

namespace SysBot.Pokemon.Discord;

/// <summary>
/// Same as <see cref="RequireRoleAccessAttribute"/> with extra consideration for bots accepting Queue requests.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class RequireQueueRoleAttribute(PokeRoutineType type) : PreconditionAttribute
{
    public override Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context, CommandInfo command, IServiceProvider services)
    {
        var mgr = SysCordSettings.Manager;
        if (mgr.Config.AllowGlobalSudo && mgr.CanUseSudo(context.User.Id))
            return Task.FromResult(PreconditionResult.FromSuccess());

        // Check if this user is a Guild User, which is the only context where roles exist
        if (context.User is not SocketGuildUser gUser)
            return Task.FromResult(PreconditionResult.FromError("You must be sending the message from a guild to run this command."));

        var roles = gUser.Roles;
        if (mgr.CanUseSudo(roles.Select(z => z.Name)))
            return Task.FromResult(PreconditionResult.FromSuccess());

        // Don't bother checking Owner/Team.

        if (!mgr.GetHasRoleAccess(type, roles.Select(z => z.Name)))
            return Task.FromResult(PreconditionResult.FromError("You do not have the required role to run this command."));

        bool canQueue = SysCordSettings.HubConfig.Queues.CanQueue;
        if (!canQueue)
            return Task.FromResult(PreconditionResult.FromError("Sorry, I am not currently accepting queue requests!"));

        return Task.FromResult(PreconditionResult.FromSuccess());
    }
}
