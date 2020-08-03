using System;
using System.Threading.Tasks;
using Discord.Commands;
using Discord.WebSocket;

namespace SysBot.AnimalCrossing
{
    public sealed class RequireSudoAttribute : PreconditionAttribute
    {
        // Override the CheckPermissions method
        public override Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context, CommandInfo command, IServiceProvider services)
        {
            var mgr = Globals.Bot.Config;
            if (mgr.CanUseSudo(context.User.Id) || context.User.Id == Globals.Self.Owner)
                return Task.FromResult(PreconditionResult.FromSuccess());

            // Check if this user is a Guild User, which is the only context where roles exist
            if (!(context.User is SocketGuildUser gUser))
                return Task.FromResult(PreconditionResult.FromError("You must be in a guild to run this command."));

            if (mgr.CanUseSudo(gUser.Id))
                return Task.FromResult(PreconditionResult.FromSuccess());

            // Since it wasn't, fail
            return Task.FromResult(PreconditionResult.FromError("You are not permitted to run this command."));
        }
    }
}