using Discord.Commands;
using Discord.WebSocket;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace SysBot.Pokemon.Discord
{
    /// <summary>
    /// Requires an assigned role in order to accept commands. Can be used by sudo users if satisfied.
    /// </summary>
    public sealed class RequireRoleAccessAttribute : PreconditionAttribute
    {
        // Create a field to store the specified name
        private readonly string _name;

        // Create a constructor so the name can be specified
        public RequireRoleAccessAttribute(string name) => _name = name;

        public override Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context, CommandInfo command, IServiceProvider services)
        {
            var mgr = SysCordSettings.Manager;
            if (mgr.Config.AllowGlobalSudo && mgr.CanUseSudo(context.User.Id))
                return Task.FromResult(PreconditionResult.FromSuccess());

            // Check if this user is a Guild User, which is the only context where roles exist
            if (context.User is not SocketGuildUser gUser)
                return Task.FromResult(PreconditionResult.FromError("이 명령을 실행하려면 길드에서 메시지를 전송해야 합니다."));

            var roles = gUser.Roles;
            if (mgr.CanUseSudo(roles.Select(z => z.Name)))
                return Task.FromResult(PreconditionResult.FromSuccess());

            if (!mgr.GetHasRoleAccess(_name, roles.Select(z => z.Name)))
                return Task.FromResult(PreconditionResult.FromError("이 명령을 실행하는 데 필요한 역할이 없습니다."));

            return Task.FromResult(PreconditionResult.FromSuccess());
        }
    }
}
