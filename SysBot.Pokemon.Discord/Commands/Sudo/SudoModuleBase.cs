using Discord;
using Discord.Interactions;

namespace SysBot.Pokemon.Discord;

[DefaultMemberPermissions(GuildPermission.PrioritySpeaker)] // basic gate to hide the commands from untrusted users, but not a full sudo check
[CommandContextType(InteractionContextType.Guild)] // must run these inside a guild, not in DMs (more auditable).
[RequireContext(ContextType.Guild)]
[RequireSudo]
public abstract class SudoModuleBase : SlashModuleBase;
