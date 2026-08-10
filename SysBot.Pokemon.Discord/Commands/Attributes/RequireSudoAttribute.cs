using System;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;

namespace SysBot.Pokemon.Discord;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class RequireSudoAttribute : PreconditionAttribute
{
    public override async Task<PreconditionResult> CheckRequirementsAsync(IInteractionContext context, ICommandInfo command, IServiceProvider services)
    {
        var mgr = SysCordSettings.Manager;
        if (mgr.Config.AllowGlobalSudo && mgr.CanUseSudo(context.User.Id))
            return PreconditionResult.FromSuccess();

        // Check if this user is a Guild User, which is the only context where roles exist
        if (context.User is not SocketGuildUser gUser)
            return PreconditionResult.FromError("You must be in a guild to run this command.");

        if (mgr.CanUseSudo(gUser.Roles.Select(z => z.Name)))
            return PreconditionResult.FromSuccess();

        // Fallback: check if it is the owner or a team member.
        if (await RequireTeamOrOwnerAttribute.IsTeamOrOwner(context).ConfigureAwait(false))
            PreconditionResult.FromSuccess();

        // Since it wasn't, fail
        return PreconditionResult.FromError("You are not permitted to run this command.");
    }
}
