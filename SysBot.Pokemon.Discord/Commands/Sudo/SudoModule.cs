using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using PKHeX.Core;

namespace SysBot.Pokemon.Discord;

[Group("sudo", "Power-user commands.")]
public class SudoModule<T> : SudoModuleBase where T : PKM, new()
{
    [SlashCommand("blacklist-user", "Blacklists a Discord user.")]
    public async Task BlackListUser(IUser user)
    {
        SysCordSettings.Settings.UserBlacklist.AddIfNew(GetReference(user));
        await RespondAsync("Done.").ConfigureAwait(false);
    }

    [SlashCommand("blacklist-comment", "Adds a comment for a blacklisted Discord user ID.")]
    public async Task BlackListComment(ulong id, string comment)
    {
        var obj = SysCordSettings.Settings.UserBlacklist.List.Find(z => z.ID == id);
        if (obj is null)
        {
            await RespondAsync($"Unable to find a user with that ID ({id}).").ConfigureAwait(false);
            return;
        }

        var old = obj.Comment;
        obj.Comment = comment;
        await RespondAsync($"Done. Changed existing comment ({old}) to ({comment}).").ConfigureAwait(false);
    }

    [SlashCommand("unblacklist-user", "Removes a Discord user from the blacklist.")]
    public async Task UnBlackListUser(IUser user)
    {
        SysCordSettings.Settings.UserBlacklist.RemoveAll(z => z.ID == user.Id);
        await RespondAsync("Done.").ConfigureAwait(false);
    }

    [SlashCommand("blacklist-ids", "Blacklists comma-separated Discord user IDs.")]
    public async Task BlackListIDs(string ids)
    {
        SysCordSettings.Settings.UserBlacklist.AddIfNew(GetIDs(ids).Select(z => GetReference(z, nameof(BlackListIDs))));
        await RespondAsync("Done.").ConfigureAwait(false);
    }

    [SlashCommand("unblacklist-ids", "Removes comma-separated Discord user IDs from the blacklist.")]
    public async Task UnBlackListIDs(string ids)
    {
        var set = GetIDs(ids).ToHashSet();
        SysCordSettings.Settings.UserBlacklist.RemoveAll(z => set.Contains(z.ID));
        await RespondAsync("Done.").ConfigureAwait(false);
    }

    [SlashCommand("blacklist-summary", "Prints the list of blacklisted Discord users.")]
    public async Task PrintBlacklist()
    {
        await RespondAsync(Format.Code(string.Join('\n', SysCordSettings.Settings.UserBlacklist.Summarize()))).ConfigureAwait(false);
    }

    [SlashCommand("ban-ids", "Bans comma-separated online user IDs.")]
    public async Task BanOnlineIDs(string ids)
    {
        SysCord<T>.Runner.Hub.Config.TradeAbuse.BannedIDs.AddIfNew(GetIDs(ids).Select(z => GetReference(z, nameof(BanOnlineIDs))));
        await RespondAsync("Done.").ConfigureAwait(false);
    }

    [SlashCommand("banned-id-comment", "Adds a comment for a banned online user ID.")]
    public async Task BanOnlineIDComment(ulong id, string comment)
    {
        var obj = SysCord<T>.Runner.Hub.Config.TradeAbuse.BannedIDs.List.Find(z => z.ID == id);
        if (obj is null)
        {
            await RespondAsync($"Unable to find a user with that online ID ({id}).").ConfigureAwait(false);
            return;
        }
        var old = obj.Comment;
        obj.Comment = comment;
        await RespondAsync($"Done. Changed existing comment ({old}) to ({comment}).").ConfigureAwait(false);
    }

    [SlashCommand("unban-ids", "Removes comma-separated online IDs from the ban list.")]
    public async Task UnBanOnlineIDs(string ids)
    {
        var set = GetIDs(ids).ToHashSet();
        SysCord<T>.Runner.Hub.Config.TradeAbuse.BannedIDs.RemoveAll(z => set.Contains(z.ID));
        await RespondAsync("Done.").ConfigureAwait(false);
    }

    [SlashCommand("banned-id-summary", "Prints the list of banned online IDs.")]
    public async Task PrintBannedOnlineIDs()
    {
        await RespondAsync(Format.Code(string.Join('\n', SysCord<T>.Runner.Hub.Config.TradeAbuse.BannedIDs.Summarize()))).ConfigureAwait(false);
    }

    [SlashCommand("forget-user", "Forgets previously encountered online IDs.")]
    public async Task ForgetPreviousUser(string ids)
    {
        foreach (var id in GetIDs(ids))
        {
            PokeRoutineExecutorBase.PreviousUsers.RemoveAllNID(id);
            PokeRoutineExecutorBase.PreviousUsersDistribution.RemoveAllNID(id);
        }
        await RespondAsync("Done.").ConfigureAwait(false);
    }

    [SlashCommand("previous-user-summary", "Prints previously encountered users.")]
    public async Task PrintPreviousUsers()
    {
        var messages = new List<string>();
        List<string> lines = [.. PokeRoutineExecutorBase.PreviousUsers.Summarize()];
        if (lines.Count != 0)
            messages.Add(Format.Code("Previous Users:\n" + string.Join('\n', lines)));

        lines = [.. PokeRoutineExecutorBase.PreviousUsersDistribution.Summarize()];
        if (lines.Count != 0)
            messages.Add(Format.Code("Previous Distribution Users:\n" + string.Join('\n', lines)));

        await RespondAsync(messages.Count == 0 ? "No previous users found." : string.Join('\n', messages)).ConfigureAwait(false);
    }

    private static IEnumerable<ulong> GetIDs(string content)
    {
        return content.Split([",", ", ", " "], StringSplitOptions.RemoveEmptyEntries)
            .Select(z => ulong.TryParse(z, out var x) ? x : 0).Where(z => z != 0);
    }

    [SlashCommand("pool-reload", "Reloads the bot pool from the configured folder.")]
    public async Task ReloadPoolAsync()
    {
        var hub = SysCord<T>.Runner.Hub;
        var ok = hub.Ledy.Pool.Reload(hub.Config.Folder.DistributeFolder);
        await RespondAsync(ok
            ? $"Reloaded from folder. Pool count: {hub.Ledy.Pool.Count}"
            : "Failed to reload from folder.").ConfigureAwait(false);
    }
}
