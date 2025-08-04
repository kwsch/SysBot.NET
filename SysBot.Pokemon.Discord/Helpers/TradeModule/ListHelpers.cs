using Discord;
using Discord.Commands;
using Discord.Net;
using PKHeX.Core;
using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace SysBot.Pokemon.Discord;

public static class ListHelpers<T> where T : PKM, new()
{
    private static TradeQueueInfo<T> Info => SysCord<T>.Runner.Hub.Queues.Info;

    public static async Task HandleListCommandAsync(SocketCommandContext context, string folderPath, string itemType,
        string commandPrefix, string args)
    {
        const int itemsPerPage = 20;
        var botPrefix = SysCord<T>.Runner.Config.Discord.CommandPrefix;

        if (string.IsNullOrEmpty(folderPath))
        {
            await Helpers<T>.ReplyAndDeleteAsync(context, "This bot does not have this feature set up.", 2);
            return;
        }

        var (filter, page) = Helpers<T>.ParseListArguments(args);

        var allFiles = Directory.GetFiles(folderPath)
            .Select(Path.GetFileNameWithoutExtension)
            .OrderBy(file => file)
            .ToList();

        var filteredFiles = allFiles
            .Where(file => string.IsNullOrWhiteSpace(filter) ||
                   file.Contains(filter, StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (filteredFiles.Count == 0)
        {
            var replyMessage = await context.Channel.SendMessageAsync($"No {itemType} found matching the filter '{filter}'.");
            _ = Helpers<T>.DeleteMessagesAfterDelayAsync(replyMessage, context.Message, 10);
            return;
        }

        var pageCount = (int)Math.Ceiling(filteredFiles.Count / (double)itemsPerPage);
        page = Math.Clamp(page, 1, pageCount);

        var pageItems = filteredFiles.Skip((page - 1) * itemsPerPage).Take(itemsPerPage);

        var embed = new EmbedBuilder()
            .WithTitle($"Available {char.ToUpper(itemType[0]) + itemType[1..]} - Filter: '{filter}'")
            .WithDescription($"Page {page} of {pageCount}")
            .WithColor(Color.Blue);

        foreach (var item in pageItems)
        {
            var index = allFiles.IndexOf(item) + 1;
            embed.AddField($"{index}. {item}", $"Use `{botPrefix}{commandPrefix} {index}` to request this {itemType.TrimEnd('s')}.");
        }

        await SendDMOrReplyAsync(context, embed.Build());
    }

    public static async Task SendDMOrReplyAsync(SocketCommandContext context, Embed embed)
    {
        IUserMessage replyMessage;

        if (context.User is IUser user)
        {
            try
            {
                var dmChannel = await user.CreateDMChannelAsync();
                await dmChannel.SendMessageAsync(embed: embed);
                replyMessage = await context.Channel.SendMessageAsync($"{context.User.Mention}, I've sent you a DM with the list.");
            }
            catch (HttpException ex) when (ex.HttpCode == HttpStatusCode.Forbidden)
            {
                replyMessage = await context.Channel.SendMessageAsync($"{context.User.Mention}, I'm unable to send you a DM. Please check your **Server Privacy Settings**.");
            }
        }
        else
        {
            replyMessage = await context.Channel.SendMessageAsync("**Error**: Unable to send a DM. Please check your **Server Privacy Settings**.");
        }

        _ = Helpers<T>.DeleteMessagesAfterDelayAsync(replyMessage, context.Message, 10);
    }

    public static async Task HandleRequestCommandAsync(SocketCommandContext context, string folderPath, int index,
        string itemType, string listCommand)
    {
        var userID = context.User.Id;
        if (!await Helpers<T>.EnsureUserNotInQueueAsync(userID))
        {
            await Helpers<T>.ReplyAndDeleteAsync(context,
                "You already have an existing trade in the queue that cannot be cleared. Please wait until it is processed.", 2);
            return;
        }

        try
        {
            if (string.IsNullOrEmpty(folderPath))
            {
                await Helpers<T>.ReplyAndDeleteAsync(context, "This bot does not have this feature set up.", 2);
                return;
            }

            var files = Directory.GetFiles(folderPath)
                .Select(Path.GetFileName)
                .OrderBy(x => x)
                .ToList();

            if (index < 1 || index > files.Count)
            {
                await Helpers<T>.ReplyAndDeleteAsync(context,
                    $"Invalid {itemType} index. Please use a valid number from the `.{listCommand}` command.", 2);
                return;
            }

            var selectedFile = files[index - 1];
            var fileData = await File.ReadAllBytesAsync(Path.Combine(folderPath, selectedFile));
            var download = new Download<PKM>
            {
                Data = EntityFormat.GetFromBytes(fileData),
                Success = true
            };

            var pk = Helpers<T>.GetRequest(download);
            if (pk == null)
            {
                await Helpers<T>.ReplyAndDeleteAsync(context,
                    $"Failed to convert {itemType} file to the required PKM type.", 2);
                return;
            }

            var code = Info.GetRandomTradeCode(userID);
            var lgcode = Info.GetRandomLGTradeCode();
            var sig = context.User.GetFavor();

            await context.Channel.SendMessageAsync($"{char.ToUpper(itemType[0]) + itemType[1..]} request added to queue.").ConfigureAwait(false);
            await Helpers<T>.AddTradeToQueueAsync(context, code, context.User.Username, pk, sig,
                context.User, lgcode: lgcode).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            await Helpers<T>.ReplyAndDeleteAsync(context, $"An error occurred: {ex.Message}", 2);
        }
        finally
        {
            if (context.Message is IUserMessage userMessage)
                _ = Helpers<T>.DeleteMessagesAfterDelayAsync(userMessage, null, 2);
        }
    }
}
