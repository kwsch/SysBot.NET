using System;
using System.Threading.Tasks;
using Discord;
using Discord.Interactions;

namespace SysBot.Pokemon.Discord;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public sealed class RequireOpenDmsAttribute : PreconditionAttribute
{
    public override Task<PreconditionResult> CheckRequirementsAsync(IInteractionContext context, ICommandInfo command, IServiceProvider services)
    {
        // This is moreso a tag to indicate to the developer that DMs are required for this command.
        // You can always obtain the DM channel, but still fail if the user has DMs off.
        // Can only know when we DM the user; let it fail then rather than send fake messages at the start of every command.
        return Task.FromResult(PreconditionResult.FromSuccess());
    }
}
