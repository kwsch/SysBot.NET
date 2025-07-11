using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Newtonsoft.Json.Linq;
using PKHeX.Core;
using System;
using System.IO;
using System.Linq;
using SysBot.Pokemon.Helpers;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using AnimatedGif;
using System.Drawing;
using Color = System.Drawing.Color;
using DiscordColor = Discord.Color;
using System.Diagnostics;

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
    // ReSharper disable once UnusedParameter.Global
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
    // ReSharper disable once UnusedParameter.Global
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
    // ReSharper disable once UnusedParameter.Global
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
    // ReSharper disable once UnusedParameter.Global
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
    // ReSharper disable once UnusedParameter.Global
    public async Task Leave()
    {
        await ReplyAsync("Goodbye.").ConfigureAwait(false);
        await Context.Guild.LeaveAsync().ConfigureAwait(false);
    }

    [Command("leaveguild")]
    [Alias("lg")]
    [Summary("Leaves guild based on supplied ID.")]
    [RequireOwner]
    // ReSharper disable once UnusedParameter.Global
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
    // ReSharper disable once UnusedParameter.Global
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

        using MemoryStream ms = new(bytes);
        var img = "cap.jpg";
        var embed = new EmbedBuilder { ImageUrl = $"attachment://{img}", Color = (DiscordColor?)Color.Purple }
            .WithFooter(new EmbedFooterBuilder { Text = $"Here's your screenshot." });

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

                var screenshotCount = 10;
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

                    using (var ms = new MemoryStream(bytes))
                    {
                        using var bitmap = new Bitmap(ms);
                        var frame = bitmap.Clone(new Rectangle(0, 0, bitmap.Width, bitmap.Height), System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                        gifFrames.Add(frame);
                    }

                    await Task.Delay(screenshotInterval).ConfigureAwait(false);
                }

                using (var ms = new MemoryStream())
                {
                    using (var gif = new AnimatedGifCreator(ms, 200))
                    {
                        foreach (var frame in gifFrames)
                        {
                            gif.AddFrame(frame);
                            frame.Dispose();
                        }
                    }

                    ms.Position = 0;
                    var gifFileName = "screenshot.gif";
                    var embed = new EmbedBuilder { ImageUrl = $"attachment://{gifFileName}", Color = (DiscordColor?)Color.Red }
                        .WithFooter(new EmbedFooterBuilder { Text = "Here's your GIF." });

                    await Context.Channel.SendFileAsync(ms, gifFileName, embed: embed.Build()).ConfigureAwait(false);
                }

                foreach (var frame in gifFrames)
                {
                    frame.Dispose();
                }
                gifFrames.Clear();
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

            var ip = config["Bots"][0]["Connection"]["IP"].ToString();
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
    // ReSharper disable once UnusedParameter.Global
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

            await Context.Message.DeleteAsync();
            await Task.Delay(TimeSpan.FromSeconds(10));
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
        var channelMentionMatch = System.Text.RegularExpressions.Regex.Match(message, @"<#(\d+)>");
        if (!channelMentionMatch.Success)
        {
            await ReplyAsync("Please mention a channel properly using #channel.");
            return;
        }
        var channelId = ulong.Parse(channelMentionMatch.Groups[1].Value);
        var actualMessage = message.Substring(0, channelMentionMatch.Index).TrimEnd();
        var channel = Context.Guild.GetChannel(channelId) as IMessageChannel;
        if (channel == null)
        {
            await ReplyAsync("Channel not found.");
            return;
        }
        // Check if the message has content or attachments
        if (string.IsNullOrWhiteSpace(actualMessage) && !hasAttachments)
        {
            await ReplyAsync("At least one of 'Content', 'Embeds', 'Components', 'Stickers' or 'Attachments' must be specified.");
            return;
        }
        // If there are attachments, send them to the channel
        if (hasAttachments)
        {
            foreach (var attachment in attachments)
            {
                using var httpClient = new HttpClient();
                var stream = await httpClient.GetStreamAsync(attachment.Url);
                await channel.SendFileAsync(stream, attachment.Filename, actualMessage);
            }
        }
        else
        {
            await channel.SendMessageAsync(actualMessage);
        }
        await Context.Message.DeleteAsync();
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

        [Command("startsysdvr")]
        [Alias("dvrstart", "startdvr", "sysdvrstart")]
        [Summary("Makes the bot open SysDVR to stream your Switch on the current PC.")]
        [RequireOwner]
        public async Task StartSysDvr()
        {
            try
            {
                var sysDvrBATPath = Path.Combine("SysDVR.bat");
                if (File.Exists(sysDvrBATPath))
                {
                    Process.Start(sysDvrBATPath);
                    await ReplyAsync("SysDVR has been initiated. You're now streaming your Switch on PC!");
                }
                else
                {
                    await ReplyAsync("**SysDVR.bat** cannot be found at the specified location.");
                }
            }
            catch (Exception ex)
            {
                await ReplyAsync($"**SysDVR Error:** {ex.Message}");
            }
        }

        [Command("sysdvr")]
        [Alias("stream")]
        [Summary("Displays instructions on how to use SysDVR.")]
        [RequireOwner]
        public async Task SysDVRInstructionsAsync()
        {
            var embed0 = new EmbedBuilder()
                .WithTitle("-----------SYSDVR SETUP INSTRUCTIONS-----------");

            embed0.WithImageUrl("https://raw.githubusercontent.com/Havokx89/Bot-Sprite-Images/main/homereadybreak.png");
            var message0 = await ReplyAsync(embed: embed0.Build());


            var embed1 = new EmbedBuilder()
                .AddField("01) SETTING UP THE SYSBOT WITH SYSDVR",
                          "- [Click here](https://github.com/exelix11/SysDVR/releases) to download **SysDVR-Client-Windows-x64.7z**.\n" +
                          "- Unpack the archive and place the extracted folder anywhere you want.\n" +
                          "- Inside the folder, open **SysDVR-ClientGUI.exe.**\n" +
                          "- Select either *Video* or *Both* under the channels to stream.\n" +
                          "- Select **TCP Bridge** and enter your Switch's IP address.\n" +
                          "- Select **Create quick launch shortcut** to create a **SysDVR Launcher.bat**.\n" +
                          "- Exit the program window that launches.\n" +
                          "- Place the **SysDVR Launcher.bat** in the same folder as your SysBot.\n" +
                          "- Rename the bat file to **SysDVR.bat.**\n" +
                          "- You can then use the `dvrstart` command once you add SysDVR to your Switch.");

            embed1.WithImageUrl("https://raw.githubusercontent.com/Havokx89/Bot-Sprite-Images/main/homereadybreak.png");
            var message1 = await ReplyAsync(embed: embed1.Build());


            var embed2 = new EmbedBuilder()
                .AddField("02) SETTING UP SYSDVR ON THE SWITCH",
                          "- [Click here](https://github.com/exelix11/SysDVR/releases) to download **SysDVR.zip**.\n" +
                          "- Unpack the archive and place the extracted folders on the Switch SD card.\n" +
                          "- Reboot your Switch.\n" +
                          "- Open the SysDVR program in the Switch.\n" +
                          "- Select **TCP Bridge.**\n" +
                          "- Select **Save current mode as default.**\n" +
                          "- Select **Save and exit.**\n" +
                          "- As long as you followed Step 01, the `dvrstart` command can be used.\n");

            embed2.WithImageUrl("https://raw.githubusercontent.com/Havokx89/Bot-Sprite-Images/main/homereadybreak.png");
            var message2 = await ReplyAsync(embed: embed2.Build());

            _ = Task.Run(async () =>
            {
                await Task.Delay(90_000);
                await message0.DeleteAsync();
                await message1.DeleteAsync();
                await message2.DeleteAsync();
            });
        }

    [Command("startcontroller")]
    [Alias("controllerstart", "startcontrol", "controlstart", "startremote", "remotestart", "sbr")]
    [Summary("Makes the bot open Switch Remote for PC - a GUI game controller for your Switch.")]
    [RequireOwner]
    public async Task StartSysRemote()
    {
        try
        {
            var sysBotRemotePath = SysCord<T>.Runner.Config.SysBotRemoteFolder;

            if (Directory.Exists(sysBotRemotePath))
            {
                string executablePath = Path.Combine(sysBotRemotePath, "SwitchRemoteForPC.exe");

                if (File.Exists(executablePath))
                {
                    ProcessStartInfo startInfo = new ProcessStartInfo(executablePath)
                    {
                        WorkingDirectory = sysBotRemotePath
                    };
                    Process.Start(startInfo);

                    await ReplyAsync("Switch Remote for PC has been initiated. You can now control your Switch!");
                }
                else
                {
                    await ReplyAsync("**SwitchRemoteForPC.exe** cannot be found in the specified folder.");
                }
            }
            else
            {
                await ReplyAsync("**SwitchRemoteForPC** folder does not exist.");
            }
        }
        catch (Exception ex)
        {
            await ReplyAsync($"**SwitchRemoteForPC Error:** {ex.Message}");
        }
    }
}

