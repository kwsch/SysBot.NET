using System;
using System.Linq;
using System.Threading.Tasks;
using Discord.Commands;
using Discord.WebSocket;

namespace SysBot.Pokemon.Discord
{
    public sealed class RequireQueueRoleAttribute : PreconditionAttribute
    {
        // Create a field to store the specified name
        private readonly string _name;

        // Create a constructor so the name can be specified
        public RequireQueueRoleAttribute(string name) => _name = name;

        public override Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context, CommandInfo command, IServiceProvider services)
        {
            var mgr = SysCordInstance.Manager;
            if (mgr.Config.Discord.AllowGlobalSudo && mgr.CanUseSudo(context.User.Id))
                return Task.FromResult(PreconditionResult.FromSuccess());

            // Check if this user is a Guild User, which is the only context where roles exist
            if (!(context.User is SocketGuildUser gUser))
                return Task.FromResult(PreconditionResult.FromError("You must be in a guild to run this command."));

            var roles = gUser.Roles;
            if (mgr.CanUseSudo(roles.Select(z => z.Name)))
                return Task.FromResult(PreconditionResult.FromSuccess());

            bool canQueue = SysCordInstance.Self.Hub.Queues.Info.CanQueue;
            if (!canQueue)
                return Task.FromResult(PreconditionResult.FromError("Sorry, I am not currently accepting queue requests!"));

            if (!mgr.GetHasRoleQueue(_name, roles.Select(z => z.Name)))
                return Task.FromResult(PreconditionResult.FromError("You do not have the required role to run this command."));

            return Task.FromResult(PreconditionResult.FromSuccess());
        }
    }
}