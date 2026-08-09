using System;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Interactions;

namespace SysBot.Pokemon.Discord;

[Group("owner", "Commands usable by the bot owner.")]
[DefaultMemberPermissions(GuildPermission.Administrator)] // hide these commands from the majority of users; bot Owners must have admin on server to manage.
[RequireOwner]
public class OwnerModule : SlashModuleBase
{
    [SlashCommand("add-sudo", "Adds a user to global sudo.")]
    [CommandContextType(InteractionContextType.Guild, InteractionContextType.PrivateChannel)]
    public async Task AddSudo(IUser user)
    {
        SysCordSettings.Settings.GlobalSudoList.AddIfNew(GetReference(user));
        await RespondAsync("Done.").ConfigureAwait(false);
    }

    [SlashCommand("remove-sudo", "Removes a user from global sudo.")]
    [CommandContextType(InteractionContextType.Guild, InteractionContextType.PrivateChannel)]
    public async Task RemoveSudo(IUser user)
    {
        SysCordSettings.Settings.GlobalSudoList.RemoveAll(z => z.ID == user.Id);
        await RespondAsync("Done.").ConfigureAwait(false);
    }

    [SlashCommand("add-channel", "Adds this channel to the command whitelist.")]
    [CommandContextType(InteractionContextType.Guild)]
    public async Task AddChannel()
    {
        var c = Context.Interaction.Channel;
        SysCordSettings.Settings.ChannelWhitelist.AddIfNew(GetReference(c));
        await RespondAsync("Done.").ConfigureAwait(false);
    }

    [SlashCommand("remove-channel", "Removes this channel from the command whitelist.")]
    [CommandContextType(InteractionContextType.Guild)]
    public async Task RemoveChannel()
    {
        SysCordSettings.Settings.ChannelWhitelist.RemoveAll(z => z.ID == Context.Interaction.Channel.Id);
        await RespondAsync("Done.").ConfigureAwait(false);
    }

    [SlashCommand("leave", "Leaves the current server.")]
    [CommandContextType(InteractionContextType.Guild)]
    public async Task Leave()
    {
        await RespondAsync("Goodbye.").ConfigureAwait(false);
        if (Context.Guild is not null) await Context.Guild.LeaveAsync().ConfigureAwait(false);
    }

    [SlashCommand("leave-guild", "Leaves a guild by ID.")]
    public async Task LeaveGuild(string guildId)
    {
        if (!ulong.TryParse(guildId, out var id))
        {
            await RespondAsync("Please provide a valid Guild ID.").ConfigureAwait(false);
            return;
        }

        var guild = Context.Client.Guilds.FirstOrDefault(x => x.Id == id);
        if (guild is null)
        {
            await RespondAsync($"Provided input ({guildId}) is not a valid guild ID or the bot is not in the specified guild.").ConfigureAwait(false);
            return;
        }

        await RespondAsync($"Leaving {guild}.").ConfigureAwait(false);
        await guild.LeaveAsync().ConfigureAwait(false);
    }

    [SlashCommand("leave-all", "Leaves all servers the bot is currently in.")]
    public async Task LeaveAll()
    {
        await RespondAsync("Leaving all servers.").ConfigureAwait(false);
        foreach (var guild in Context.Client.Guilds) await guild.LeaveAsync().ConfigureAwait(false);
    }

    [SlashCommand("shutdown", "Causes the entire process to end itself.")]
    public async Task ExitProgram()
    {
        await RespondAsync("Shutting down... goodbye! **Bot services are going offline.**").ConfigureAwait(false);
        Environment.Exit(0);
    }
}
