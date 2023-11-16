using Discord.Commands;
using Discord.WebSocket;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace SysBot.Pokemon.Discord;

public sealed class RequireRoleAttribute(string RoleName) : PreconditionAttribute
{
    // Create a field to store the specified name

    // Create a constructor so the name can be specified

    // Override the CheckPermissions method
    public override Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context, CommandInfo command, IServiceProvider services)
    {
        // Since no async work is done, the result has to be wrapped with `Task.FromResult` to avoid compiler errors

        // Check if this user is a Guild User, which is the only context where roles exist
        if (context.User is not SocketGuildUser gUser)
            return Task.FromResult(PreconditionResult.FromError("You must be in a guild to run this command."));

        // If this command was executed by a user with the appropriate role, return a success
        if (gUser.Roles.Any(r => r.Name == RoleName))
            return Task.FromResult(PreconditionResult.FromSuccess());

        // Since it wasn't, fail
        return Task.FromResult(PreconditionResult.FromError($"You must have a role named {RoleName} to run this command."));
    }
}
