using System;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;

namespace SysBot.Pokemon.Discord;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class RequireTeamOrOwnerAttribute : PreconditionAttribute
{
    public override async Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context, CommandInfo command, IServiceProvider services)
    {
        if (await IsTeamOrOwner(context).ConfigureAwait(false))
            PreconditionResult.FromSuccess();

        return PreconditionResult.FromError("You are not permitted to run this command.");
    }

    public static async Task<bool> IsTeamOrOwner(ICommandContext context)
    {
        // Get application info from the client
        var appInfo = await context.Client.GetApplicationInfoAsync().ConfigureAwait(false);

        // Owner always gets permission.
        if (context.User.Id == appInfo.Owner.Id)
            return true;

        // Check if the bot is owned by a team; if so, any marked as Owner are permitted.
        if (appInfo.Team is { } team && team.TeamMembers.Any(m => m.Role == TeamRole.Owner))
            return true;

        // Return a silent failure if unauthorized (keeps help menus clean).
        return false;
    }
}
