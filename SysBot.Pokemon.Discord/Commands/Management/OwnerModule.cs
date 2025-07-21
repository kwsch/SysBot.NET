using AnimatedGif;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Newtonsoft.Json.Linq;
using PKHeX.Core;
using SysBot.Pokemon.Helpers;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Color = System.Drawing.Color;
using DiscordColor = Discord.Color;

namespace SysBot.Pokemon.Discord;

public class OwnerModule<T> : SudoModule<T> where T : PKM, new()
{
    [Command("listguilds")]
    [Alias("lg", "servers", "listservers")]
    [Summary("Lists all guilds the bot is part of.")]
    [RequireSudo]
    public async Task ListGuilds(int page = 1)
    {
        const int guildsPerPage = 25; // Discord limit for fields in an embed
        int guildCount = Context.Client.Guilds.Count;
        int totalPages = (int)Math.Ceiling(guildCount / (double)guildsPerPage);
        page = Math.Max(1, Math.Min(page, totalPages));

        var guilds = Context.Client.Guilds
            .Skip((page - 1) * guildsPerPage)
            .Take(guildsPerPage);

        var embedBuilder = new EmbedBuilder()
            .WithTitle($"List of Guilds - Page {page}/{totalPages}")
            .WithDescription("Here are the guilds I'm currently in:")
            .WithColor((DiscordColor)Color.Blue);

        foreach (var guild in guilds)
        {
            embedBuilder.AddField(guild.Name, $"ID: {guild.Id}", inline: true);
        }
        var dmChannel = await Context.User.CreateDMChannelAsync();
        await dmChannel.SendMessageAsync(embed: embedBuilder.Build());

        await ReplyAsync($"{Context.User.Mention}, I've sent you a DM with the list of guilds (Page {page}).");

        if (Context.Message is IUserMessage userMessage)
        {
            await Task.Delay(2000);
            await userMessage.DeleteAsync().ConfigureAwait(false);
        }
    }

    [Command("blacklistserver")]
    [Alias("bls")]
    [Summary("Adds a server ID to the bot's server blacklist.")]
    [RequireOwner]
    public async Task BlacklistServer(ulong serverId)
    {
        var settings = SysCord<T>.Runner.Hub.Config.Discord;

        if (settings.ServerBlacklist.Contains(serverId))
        {
            await ReplyAsync("This server is already blacklisted.");
            return;
        }

        var server = Context.Client.GetGuild(serverId);
        if (server == null)
        {
            await ReplyAsync("Cannot find a server with the provided ID. Ensure the bot is a member of the server you wish to blacklist.");
            return;
        }

        var newServerAccess = new RemoteControlAccess { ID = serverId, Name = server.Name, Comment = "Blacklisted server" };

        settings.ServerBlacklist.AddIfNew([newServerAccess]);

        await server.LeaveAsync();
        await ReplyAsync($"Left the server '{server.Name}' and added it to the blacklist.");
    }

    [Command("unblacklistserver")]
    [Alias("ubls")]
    [Summary("Removes a server ID from the bot's server blacklist.")]
    [RequireOwner]
    public async Task UnblacklistServer(ulong serverId)
    {
        var settings = SysCord<T>.Runner.Hub.Config.Discord;

        if (!settings.ServerBlacklist.Contains(serverId))
        {
            await ReplyAsync("This server is not currently blacklisted.");
            return;
        }

        var wasRemoved = settings.ServerBlacklist.RemoveAll(x => x.ID == serverId) > 0;

        if (wasRemoved)
        {
            await ReplyAsync($"Server with ID {serverId} has been removed from the blacklist.");
        }
        else
        {
            await ReplyAsync("An error occurred while trying to remove the server from the blacklist. Please check the server ID and try again.");
        }
    }

    [Command("addSudo")]
    [Summary("Adds mentioned user to global sudo")]
    [RequireOwner]
    public async Task SudoUsers([Remainder] string _)
    {
        var users = Context.Message.MentionedUsers;
        var objects = users.Select(GetReference);
        SysCordSettings.Settings.GlobalSudoList.AddIfNew(objects);
        await ReplyAsync("Done.").ConfigureAwait(false);
    }

    [Command("removeSudo")]
    [Summary("Removes mentioned user from global sudo")]
    [RequireOwner]
    public async Task RemoveSudoUsers([Remainder] string _)
    {
        var users = Context.Message.MentionedUsers;
        var objects = users.Select(GetReference);
        SysCordSettings.Settings.GlobalSudoList.RemoveAll(z => objects.Any(o => o.ID == z.ID));
        await ReplyAsync("Done.").ConfigureAwait(false);
    }

    [Command("addChannel")]
    [Summary("Adds a channel to the list of channels that are accepting commands.")]
    [RequireOwner]
    public async Task AddChannel()
    {
        var obj = GetReference(Context.Message.Channel);
        SysCordSettings.Settings.ChannelWhitelist.AddIfNew([obj]);
        await ReplyAsync("Done.").ConfigureAwait(false);
    }

    [Command("syncChannels")]
    [Alias("sch", "syncchannels")]
    [Summary("Copies all channels from ChannelWhitelist to AnnouncementChannel.")]
    [RequireOwner]
    public async Task SyncChannels()
    {
        var whitelist = SysCordSettings.Settings.ChannelWhitelist.List;
        var announcementList = SysCordSettings.Settings.AnnouncementChannels.List;

        bool changesMade = false;

        foreach (var channel in whitelist)
        {
            if (!announcementList.Any(x => x.ID == channel.ID))
            {
                announcementList.Add(channel);
                changesMade = true;
            }
        }

        if (changesMade)
        {
            await ReplyAsync("Channel whitelist has been successfully synchronized with the announcement channels.").ConfigureAwait(false);
        }
        else
        {
            await ReplyAsync("All channels from the whitelist are already in the announcement channels, no changes made.").ConfigureAwait(false);
        }
    }

    [Command("removeChannel")]
    [Summary("Removes a channel from the list of channels that are accepting commands.")]
    [RequireOwner]
    public async Task RemoveChannel()
    {
        var obj = GetReference(Context.Message.Channel);
        SysCordSettings.Settings.ChannelWhitelist.RemoveAll(z => z.ID == obj.ID);
        await ReplyAsync("Done.").ConfigureAwait(false);
    }

    [Command("leave")]
    [Alias("bye")]
    [Summary("Leaves the current server.")]
    [RequireOwner]
    public async Task Leave()
    {
        await ReplyAsync("Goodbye.").ConfigureAwait(false);
        await Context.Guild.LeaveAsync().ConfigureAwait(false);
    }

    [Command("leaveguild")]
    [Alias("lg")]
    [Summary("Leaves guild based on supplied ID.")]
    [RequireOwner]
    public async Task LeaveGuild(string userInput)
    {
        if (!ulong.TryParse(userInput, out ulong id))
        {
            await ReplyAsync("Please provide a valid Guild ID.").ConfigureAwait(false);
            return;
        }

        var guild = Context.Client.Guilds.FirstOrDefault(x => x.Id == id);
        if (guild is null)
        {
            await ReplyAsync($"Provided input ({userInput}) is not a valid guild ID or the bot is not in the specified guild.").ConfigureAwait(false);
            return;
        }

        await ReplyAsync($"Leaving {guild}.").ConfigureAwait(false);
        await guild.LeaveAsync().ConfigureAwait(false);
    }

    [Command("leaveall")]
    [Summary("Leaves all servers the bot is currently in.")]
    [RequireOwner]
    public async Task LeaveAll()
    {
        await ReplyAsync("Leaving all servers.").ConfigureAwait(false);
        foreach (var guild in Context.Client.Guilds)
        {
            await guild.LeaveAsync().ConfigureAwait(false);
        }
    }

    [Command("repeek")]
    [Alias("peek")]
    [Summary("Take and send a screenshot from the currently configured Switch.")]
    [RequireSudo]
    public async Task RePeek()
    {
        string ip = OwnerModule<T>.GetBotIPFromJsonConfig();
        var source = new CancellationTokenSource();
        var token = source.Token;

        var bot = SysCord<T>.Runner.GetBot(ip);
        if (bot == null)
        {
            await ReplyAsync($"No bot found with the specified IP address ({ip}).").ConfigureAwait(false);
            return;
        }

        _ = Array.Empty<byte>();
        byte[]? bytes;
        try
        {
            bytes = await bot.Bot.Connection.PixelPeek(token).ConfigureAwait(false) ?? [];
        }
        catch (Exception ex)
        {
            await ReplyAsync($"Error while fetching pixels: {ex.Message}");
            return;
        }

        if (bytes.Length == 0)
        {
            await ReplyAsync("No screenshot data received.");
            return;
        }

        await using MemoryStream ms = new(bytes);
        const string img = "cap.jpg";
        var embed = new EmbedBuilder { ImageUrl = $"attachment://{img}", Color = (DiscordColor?)Color.Purple }
            .WithFooter(new EmbedFooterBuilder { Text = "Here's your screenshot." });

        await Context.Channel.SendFileAsync(ms, img, embed: embed.Build());
    }

    [Command("video")]
    [Alias("video")]
    [Summary("Take and send a GIF from the currently configured Switch.")]
    [RequireSudo]
    public async Task RePeekGIF()
    {
        await Context.Channel.SendMessageAsync("Processing GIF request...").ConfigureAwait(false);

        // Offload processing to a separate task so we dont hold up gateway tasks
        _ = Task.Run(async () =>
        {
            try
            {
                string ip = OwnerModule<T>.GetBotIPFromJsonConfig();
                var source = new CancellationTokenSource();
                var token = source.Token;
                var bot = SysCord<T>.Runner.GetBot(ip);

                if (bot == null)
                {
                    await ReplyAsync($"No bot found with the specified IP address ({ip}).").ConfigureAwait(false);
                    return;
                }

                const int screenshotCount = 10;
                var screenshotInterval = TimeSpan.FromSeconds(0.1 / 10);
#pragma warning disable CA1416 // Validate platform compatibility
                var gifFrames = new List<System.Drawing.Image>();
#pragma warning restore CA1416 // Validate platform compatibility

                for (int i = 0; i < screenshotCount; i++)
                {
                    byte[] bytes;
                    try
                    {
                        bytes = await bot.Bot.Connection.PixelPeek(token).ConfigureAwait(false) ?? Array.Empty<byte>();
                    }
                    catch (Exception ex)
                    {
                        await ReplyAsync($"Error while fetching pixels: {ex.Message}").ConfigureAwait(false);
                        return;
                    }

                    if (bytes.Length == 0)
                    {
                        await ReplyAsync("No screenshot data received.").ConfigureAwait(false);
                        return;
                    }

                    await using (var ms = new MemoryStream(bytes))
                    {
#pragma warning disable CA1416 // Validate platform compatibility
                        using var bitmap = new Bitmap(ms);
#pragma warning restore CA1416 // Validate platform compatibility
#pragma warning disable CA1416 // Validate platform compatibility
                        var frame = bitmap.Clone(new Rectangle(0, 0, bitmap.Width, bitmap.Height), System.Drawing.Imaging.PixelFormat.Format32bppArgb);
#pragma warning restore CA1416 // Validate platform compatibility
#pragma warning disable CA1416 // Validate platform compatibility
                        gifFrames.Add(frame);
#pragma warning restore CA1416 // Validate platform compatibility
                    }

                    await Task.Delay(screenshotInterval).ConfigureAwait(false);
                }

                await using (var ms = new MemoryStream())
                {
                    using (var gif = new AnimatedGifCreator(ms, 200))
                    {
                        foreach (var frame in gifFrames)
                        {
                            gif.AddFrame(frame);
#pragma warning disable CA1416 // Validate platform compatibility
                            frame.Dispose();
#pragma warning restore CA1416 // Validate platform compatibility
                        }
                    }

                    ms.Position = 0;
                    const string gifFileName = "screenshot.gif";
                    var embed = new EmbedBuilder { ImageUrl = $"attachment://{gifFileName}", Color = (DiscordColor?)Color.Red }
                        .WithFooter(new EmbedFooterBuilder { Text = "Here's your GIF." });

                    await Context.Channel.SendFileAsync(ms, gifFileName, embed: embed.Build()).ConfigureAwait(false);
                }

                foreach (var frame in gifFrames)
                {
#pragma warning disable CA1416 // Validate platform compatibility
                    frame.Dispose();
#pragma warning restore CA1416 // Validate platform compatibility
                }
#pragma warning disable CA1416 // Validate platform compatibility
                gifFrames.Clear();
#pragma warning restore CA1416 // Validate platform compatibility
            }
            catch (Exception ex)
            {
                await ReplyAsync($"Error while processing GIF: {ex.Message}").ConfigureAwait(false);
            }
        });
    }

    private static string GetBotIPFromJsonConfig()
    {
        try
        {
            var jsonData = File.ReadAllText(TradeBot.ConfigPath);
            var config = JObject.Parse(jsonData);

#pragma warning disable CS8602 // Dereference of a possibly null reference.
            var ip = config["Bots"][0]["Connection"]["IP"].ToString();
#pragma warning restore CS8602 // Dereference of a possibly null reference.
            return ip;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error reading config file: {ex.Message}");
            return "192.168.1.1";
        }
    }

    [Command("kill")]
    [Alias("shutdown")]
    [Summary("Causes the entire process to end itself!")]
    [RequireOwner]
    public async Task ExitProgram()
    {
        await Context.Channel.EchoAndReply("Shutting down... goodbye! **Bot services are going offline.**").ConfigureAwait(false);
        Environment.Exit(0);
    }

    [Command("dm")]
    [Summary("Sends a direct message to a specified user.")]
    [RequireOwner]
    public async Task DMUserAsync(SocketUser user, [Remainder] string message)
    {
        var attachments = Context.Message.Attachments;
        var hasAttachments = attachments.Count != 0;

        var embed = new EmbedBuilder
        {
            Title = "Private Message from the Bot Owner",
            Description = message,
            Color = (DiscordColor?)Color.Gold,
            Timestamp = DateTimeOffset.Now,
            ThumbnailUrl = "https://raw.githubusercontent.com/Havokx89/Bot-Sprite-Images/main/pikamail.png"
        };

        try
        {
            var dmChannel = await user.CreateDMChannelAsync();

            if (hasAttachments)
            {
                foreach (var attachment in attachments)
                {
                    using var httpClient = new HttpClient();
                    var stream = await httpClient.GetStreamAsync(attachment.Url);
                    var file = new FileAttachment(stream, attachment.Filename);
                    await dmChannel.SendFileAsync(file, embed: embed.Build());
                }
            }
            else
            {
                await dmChannel.SendMessageAsync(embed: embed.Build());
            }

            var confirmationMessage = await ReplyAsync($"Message successfully sent to {user.Username}.");
            await Context.Message.DeleteAsync();
            await Task.Delay(TimeSpan.FromSeconds(10));
            await confirmationMessage.DeleteAsync();
        }
        catch (Exception ex)
        {
            await ReplyAsync($"Failed to send message to {user.Username}. Error: {ex.Message}");
        }
    }

    [Command("say")]
    [Summary("Sends a message to a specified channel.")]
    [RequireSudo]
    public async Task SayAsync([Remainder] string message)
    {
        var attachments = Context.Message.Attachments;
        var hasAttachments = attachments.Count != 0;

        var indexOfChannelMentionStart = message.LastIndexOf('<');
        var indexOfChannelMentionEnd = message.LastIndexOf('>');
        if (indexOfChannelMentionStart == -1 || indexOfChannelMentionEnd == -1)
        {
            await ReplyAsync("Please mention a channel properly using #channel.");
            return;
        }

        var channelMention = message.Substring(indexOfChannelMentionStart, indexOfChannelMentionEnd - indexOfChannelMentionStart + 1);
        var actualMessage = message.Substring(0, indexOfChannelMentionStart).TrimEnd();

        var channel = Context.Guild.Channels.FirstOrDefault(c => $"<#{c.Id}>" == channelMention);

        if (channel == null)
        {
            await ReplyAsync("Channel not found.");
            return;
        }

        if (channel is not IMessageChannel messageChannel)
        {
            await ReplyAsync("The mentioned channel is not a text channel.");
            return;
        }

        // If there are attachments, send them to the channel
        if (hasAttachments)
        {
            foreach (var attachment in attachments)
            {
                using var httpClient = new HttpClient();
                var stream = await httpClient.GetStreamAsync(attachment.Url);
                var file = new FileAttachment(stream, attachment.Filename);
                await messageChannel.SendFileAsync(file, actualMessage);
            }
        }
        else
        {
            await messageChannel.SendMessageAsync(actualMessage);
        }

        // Send confirmation message to the user
        await ReplyAsync($"Message successfully posted in {channelMention}.");
    }

    private RemoteControlAccess GetReference(IUser channel) => new()
    {
        ID = channel.Id,
        Name = channel.Username,
        Comment = $"Added by {Context.User.Username} on {DateTime.Now:yyyy.MM.dd-hh:mm:ss}",
    };

    private RemoteControlAccess GetReference(IChannel channel) => new()
    {
        ID = channel.Id,
        Name = channel.Name,
        Comment = $"Added by {Context.User.Username} on {DateTime.Now:yyyy.MM.dd-hh:mm:ss}",
    };
}
