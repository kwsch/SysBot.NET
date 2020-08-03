using System;
using System.Linq;
using System.Threading.Tasks;
using Discord.Commands;
using Discord.WebSocket;

namespace SysBot.AnimalCrossing
{
    public static class Globals
    {
        public static SysCord Self = default!;
        public static CrossBot Bot = default!;
    }

    public sealed class RequireQueueRoleAttribute : PreconditionAttribute
    {
        // Create a field to store the specified name
        private readonly string _name;

        // Create a constructor so the name can be specified
        public RequireQueueRoleAttribute(string name) => _name = name;

        public override Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context, CommandInfo command, IServiceProvider services)
        {
            var mgr = Globals.Bot.Config;
            if (mgr.CanUseSudo(context.User.Id) || Globals.Self.Owner == context.User.Id)
                return Task.FromResult(PreconditionResult.FromSuccess());

            // Check if this user is a Guild User, which is the only context where roles exist
            if (!(context.User is SocketGuildUser gUser))
                return Task.FromResult(PreconditionResult.FromError("You must be in a guild to run this command."));

            if (!mgr.AcceptingCommands)
                return Task.FromResult(PreconditionResult.FromError("Sorry, I am not currently accepting commands!"));

            bool hasRole = mgr.GetHasRole(_name, gUser.Roles.Select(z => z.Name));
            if (!hasRole)
                return Task.FromResult(PreconditionResult.FromError("You do not have the required role to run this command."));

            return Task.FromResult(PreconditionResult.FromSuccess());
        }
    }
}
