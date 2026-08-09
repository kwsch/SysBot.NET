using Discord;
using Discord.Interactions;

namespace SysBot.Pokemon.Discord;

[RequireUserPermission(ChannelPermission.BypassSlowmode)] // basic gate to hide the commands from untrusted users, but not a full sudo check
[CommandContextType(InteractionContextType.Guild)] // must run these inside a guild, not in DMs (more auditable).
[RequireContext(ContextType.Guild)]
public abstract class SudoModuleBase : SlashModuleBase;
