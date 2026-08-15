using System;
using System.Threading.Tasks;
using Discord;
using Discord.Interactions;

namespace SysBot.Pokemon.Discord;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class RequireTeamOrOwnerAttribute : PreconditionAttribute
{
    public override Task<PreconditionResult> CheckRequirementsAsync(IInteractionContext context, ICommandInfo command, IServiceProvider services)
    {
        if (IsTeamOrOwner(context))
            return Task.FromResult(PreconditionResult.FromSuccess());

        return Task.FromResult(PreconditionResult.FromError("You are not permitted to run this command."));
    }

    public static bool IsTeamOrOwner(IInteractionContext context)
    {
        // Check if the bot is owned by a team; if so, any marked as Owner are permitted.
        return SysCordSettings.Manager.IsTeamOrOwner(context.User.Id);
    }
}
