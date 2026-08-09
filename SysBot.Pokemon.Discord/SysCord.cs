using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using PKHeX.Core;
using SysBot.Base;
using static Discord.GatewayIntents;

namespace SysBot.Pokemon.Discord;

public sealed class SysCord<T> where T : PKM, new()
{
    public static PokeBotRunner<T> Runner { get; private set; } = null!;

    private readonly DiscordSocketClient _client;
    private readonly DiscordManager _manager;
    public readonly PokeTradeHub<T> Hub;

    private readonly InteractionService _interactions;
    private readonly IServiceProvider _services;

    private bool MessageChannelsLoaded { get; set; }
    private bool CommandsRegistered { get; set; }

    public SysCord(PokeBotRunner<T> runner)
    {
        Runner = runner;
        Hub = runner.Hub;
        _manager = new DiscordManager(Hub.Config.Discord);

        SysCordSettings.Manager = _manager;
        SysCordSettings.HubConfig = Hub.Config;

        _client = new DiscordSocketClient(new DiscordSocketConfig
        {
            LogLevel = LogSeverity.Info,
            GatewayIntents = Guilds | GuildMessages | DirectMessages,
        });

        _interactions = new InteractionService(_client.Rest, new InteractionServiceConfig
        {
            LogLevel = LogSeverity.Info,
            DefaultRunMode = Hub.Config.Discord.AsyncCommands ? RunMode.Async : RunMode.Sync,
        });

        _client.Log += Log;
        _interactions.Log += Log;
        _services = ConfigureServices();
    }

    private ServiceProvider ConfigureServices()
    {
        return new ServiceCollection()
            .AddSingleton(_client)
            .AddSingleton(_interactions)
            .BuildServiceProvider();
    }

    private static Task Log(LogMessage msg)
    {
        var text = $"[{msg.Severity,8}] {msg.Source}: {msg.Message} {msg.Exception}";
        Console.ForegroundColor = GetTextColor(msg.Severity);
        Console.WriteLine($"{DateTime.Now,-19} {text}");
        Console.ResetColor();
        LogUtil.LogText($"SysCord: {text}");
        return Task.CompletedTask;
    }

    private static ConsoleColor GetTextColor(LogSeverity sv) => sv switch
    {
        LogSeverity.Critical => ConsoleColor.Red,
        LogSeverity.Error => ConsoleColor.Red,
        LogSeverity.Warning => ConsoleColor.Yellow,
        LogSeverity.Info => ConsoleColor.White,
        LogSeverity.Verbose => ConsoleColor.DarkGray,
        LogSeverity.Debug => ConsoleColor.DarkGray,
        _ => Console.ForegroundColor,
    };

    public async Task MainAsync(string apiToken, CancellationToken token)
    {
        await InitCommands().ConfigureAwait(false);
        await _client.LoginAsync(TokenType.Bot, apiToken).ConfigureAwait(false);

        var app = await _client.GetApplicationInfoAsync().ConfigureAwait(false);
        _manager.Owner = app.Owner.Id;

        await _client.StartAsync().ConfigureAwait(false);
        await MonitorStatusAsync(token).ConfigureAwait(false);
    }

    public async Task InitCommands()
    {
        // All modules are suffixed with "Module" in their class name.
        // The blacklist is a comma-separated list of module names (without the "Module" suffix) that should not be added to the bot.
        var blacklist = Hub.Config.Discord.ModuleBlacklist
            .Replace("Module", "")
            .Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(z => z.Trim())
            .ToList();

        var assembly = Assembly.GetExecutingAssembly();
        await LoadSlashCommandsFromAssembly(assembly, blacklist).ConfigureAwait(false);

        _client.Ready += LoadCommandsAndChannels;
        _client.InteractionCreated += HandleInteractionAsync;
    }

    private async Task LoadSlashCommandsFromAssembly(Assembly assembly, List<string> blacklist)
    {
        var moduleTypes = assembly.DefinedTypes
            .Where(z => z is { IsAbstract: false, IsGenericTypeDefinition: false } && typeof(InteractionModuleBase<SocketInteractionContext>).IsAssignableFrom(z.AsType()))
            .Select(z => z.AsType());
        var genericTypes = assembly.DefinedTypes
            .Where(z => z is { IsAbstract: false, IsGenericTypeDefinition: true } && typeof(InteractionModuleBase<SocketInteractionContext>).IsAssignableFrom(z.AsType()))
            .Select(z => z.MakeGenericType(typeof(T)));

        var types = moduleTypes.Concat(genericTypes);

        foreach (var module in types)
        {
            if (!IsBlacklisted(module, blacklist))
                await _interactions.AddModuleAsync(module, _services).ConfigureAwait(false);
        }
    }

    private static bool IsBlacklisted(Type module, List<string> blacklist)
    {
        var name = GetModuleName(module.Name);
        return blacklist.Any(z => z.Equals(name, StringComparison.OrdinalIgnoreCase));
    }

    private static string GetModuleName(string name)
    {
        name = name.Replace("Module", "");
        // Trim off any generic type parameters (e.g., `1, `2) from the name for comparison purposes.
        var gen = name.IndexOf('`');
        if (gen != -1)
            name = name[..gen];
        return name;
    }

    private async Task HandleInteractionAsync(SocketInteraction interaction)
    {
        // Only application commands are relevant here. Keeping this check means
        // future component/modal interactions can be handled independently.
        if (interaction is not SocketSlashCommand command)
            return;

        var context = new SocketInteractionContext(_client, command);

        if (!_manager.CanUseCommandUser(context.User.Id))
        {
            await RespondErrorAsync(command, "You are not permitted to use this command.", ephemeral: true).ConfigureAwait(false);
            return;
        }

        if ((context.Interaction.ChannelId is not {  } channel) || (!_manager.CanUseCommandChannel(channel) && context.User.Id != _manager.Owner))
        {
            // Visibly reply if settings require (so that others can see).
            var ephemeral = !Hub.Config.Discord.ReplyCannotUseCommandInChannel;
            await RespondErrorAsync(command, "You can't use that command here.", ephemeral: ephemeral).ConfigureAwait(false);
            return;
        }

        var commandName = command.Data.Name;
        var location = context.Interaction.IsDMInteraction ? "Direct Messages" : context.Guild?.Name ?? "Unknown Guild";
        var status = $"Executing command from {location}#{context.Interaction.Channel?.Name ?? $"Unknown Channel: {channel}"}:@{context.User.Username}. Command: {commandName}";
        await Log(GetLog(LogSeverity.Info, status)).ConfigureAwait(false);

        var result = await _interactions.ExecuteCommandAsync(context, _services).ConfigureAwait(false);
        if (!result.IsSuccess && !command.HasResponded)
            await RespondErrorAsync(command, result.ErrorReason).ConfigureAwait(false);
    }

    private static Task RespondErrorAsync(SocketInteraction interaction, string message, bool ephemeral = true) => interaction.HasResponded
        ? interaction.FollowupAsync(message, ephemeral: ephemeral)
        : interaction.RespondAsync(message, ephemeral: ephemeral);

    private static LogMessage GetLog(LogSeverity severity, string message, [CallerMemberName] string identity = "")
        => new (severity, identity, message);

    private async Task MonitorStatusAsync(CancellationToken token)
    {
        const int interval = 20;
        var state = UserStatus.Idle;

        while (!token.IsCancellationRequested)
        {
            var time = DateTime.Now;
            var lastLogged = LogUtil.LastLogged;
            if (Hub.Config.Discord.BotColorStatusTradeOnly)
            {
                var recent = Hub.Bots.ToArray()
                    .Where(z => z.Config.InitialRoutine.IsTradeBot())
                    .MaxBy(z => z.LastTime);
                lastLogged = recent?.LastTime ?? time;
            }
            var delta = time - lastLogged;
            var gap = TimeSpan.FromSeconds(interval) - delta;

            bool noQueue = !Hub.Queues.Info.GetCanQueue();
            if (gap <= TimeSpan.Zero)
            {
                var idle = noQueue ? UserStatus.DoNotDisturb : UserStatus.Idle;
                if (idle != state)
                {
                    state = idle;
                    await _client.SetStatusAsync(state).ConfigureAwait(false);
                }
                await Task.Delay(2_000, token).ConfigureAwait(false);
                continue;
            }

            var active = noQueue ? UserStatus.DoNotDisturb : UserStatus.Online;
            if (active != state)
            {
                state = active;
                await _client.SetStatusAsync(state).ConfigureAwait(false);
            }
            await Task.Delay(gap, token).ConfigureAwait(false);
        }
    }

    // There is a global rate limit of 200 application command creates per day, per guild
    // We'll still be good citizens and only trigger an update if the modules were revised.
    private string ComputeSlashCommandHash()
    {
        var commands = _interactions.SlashCommands;
        var json = JsonSerializer.Serialize(commands.Select(c => new {
            c.Name,
            c.Description,
            Params = c.Parameters.Select(p => new { p.Name, p.Description, p.DiscordOptionType })
        }));

        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(json)));
    }

    private async Task LoadCommandsAndChannels()
    {
        if (!CommandsRegistered)
            await RegisterSlashCommands().ConfigureAwait(false);

        if (MessageChannelsLoaded)
            return;

        // Restore Echoes
        EchoModule.RestoreChannels(_client, Hub.Config.Discord);

        // Restore Logging
        LogModule.RestoreLogging(_client, Hub.Config.Discord);
        TradeStartModule<T>.RestoreTradeStarting(_client);

        // Don't let it load more than once in case of Discord hiccups.
        const string status = "Logging and Echo channels loaded!";
        await Log(GetLog(LogSeverity.Info, status)).ConfigureAwait(false);
        MessageChannelsLoaded = true;

        var game = Hub.Config.Discord.BotGameStatus;
        if (!string.IsNullOrWhiteSpace(game))
            await _client.SetGameAsync(game).ConfigureAwait(false);
    }

    private async Task RegisterSlashCommands()
    {
        var cfg = SysCordSettings.HubConfig.Discord;
        var hash = ComputeSlashCommandHash();
        if (hash == cfg.SlashCommandHash)
        {
            LogUtil.LogInfo("Skipped registering commands -- no signature changes.");
            var count = _interactions.SlashCommands.Count;
            SysCordSettings.SetCommandsRegistered(count);
            return;
        }

        try
        {
            // deleteMissing=true removes obsolete application commands left behind by previous versions of the bot.
            // Global commands have a TTL of 1 hour
            await _interactions.RegisterCommandsGloballyAsync(deleteMissing: true).ConfigureAwait(false);
            CommandsRegistered = true;

            var count = _interactions.SlashCommands.Count;
            SysCordSettings.SetCommandsRegistered(count);
            cfg.SlashCommandHash = hash;

            var message = $"Registered {count} slash commands. Hash: {hash}";
            LogUtil.LogInfo(message);
        }
        catch (Exception ex)
        {
            LogUtil.LogSafe(ex);
        }
    }
}
