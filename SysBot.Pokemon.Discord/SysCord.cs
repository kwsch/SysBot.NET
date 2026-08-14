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
        SysCordSettings.ServiceProvider = _services;
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
        _manager.SetOwnership(app);

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
        await LoadModulesFromAssembly(assembly, blacklist).ConfigureAwait(false);

        _client.Ready += LoadCommandsAndChannels;
        _client.InteractionCreated += HandleInteractionAsync;
    }

    private async Task LoadModulesFromAssembly(Assembly assembly, IReadOnlyList<string> blacklist)
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
            var name = ReusableActions.GetModuleName(module.Name);
            if (IsBlacklisted(name, blacklist))
                continue;

            var registered = await _interactions.AddModuleAsync(module, _services).ConfigureAwait(false);
            LogUtil.LogInfo(
                $"Loaded module {name}: " +
                $"slash={registered?.SlashCommands.Count ?? -1}, " +
                $"modal={registered?.ModalCommands.Count ?? -1}, " +
                $"component={registered?.ComponentCommands.Count ?? -1}");
        }
    }

    private static bool IsBlacklisted(string name, IReadOnlyList<string> blacklist)
        => blacklist.Any(z => z.Equals(name, StringComparison.OrdinalIgnoreCase));

    private async Task HandleInteractionAsync(SocketInteraction interaction)
    {
        var context = new SocketInteractionContext(_client, interaction);

        if (!_manager.CanUseCommandUser(context.User.Id))
        {
            await RespondErrorAsync(interaction, "You are not permitted to use this command.", ephemeral: true).ConfigureAwait(false);
            return;
        }

        if ((context.Interaction.ChannelId is not { } channel) || (!_manager.CanUseCommandChannel(channel) && _manager.IsTeamOrOwner(context.User.Id)))
        {
            // Visibly reply if settings require (so that others can see).
            var ephemeral = !Hub.Config.Discord.ReplyCannotUseCommandInChannel;
            await RespondErrorAsync(interaction, "You can't use that here.", ephemeral: ephemeral).ConfigureAwait(false);
            return;
        }

        await LogInteractionStart(context, channel).ConfigureAwait(false);

        var result = await _interactions.ExecuteCommandAsync(context, _services).ConfigureAwait(false);
        if (!result.IsSuccess && !interaction.HasResponded)
            await RespondErrorAsync(interaction, result.ErrorReason).ConfigureAwait(false);
    }

    private static async Task LogInteractionStart(SocketInteractionContext context, ulong channelId)
    {
        var interaction = context.Interaction;
        var (type, identity) = interaction switch
        {
            SocketSlashCommand cmd => ("slash command", $"Command: {cmd.CommandName}"),
            SocketModal modal => ("modal", $"Modal: {modal.Id}"),
            SocketMessageComponent c => ("component", $"Component: {c.Id}"),
            _ => (interaction.Type.ToString(), "Unknown"),
        };
        var channel = interaction.Channel?.Name ?? $"Unknown Channel: {channelId}";
        var location = interaction.IsDMInteraction ? "Direct Messages" : context.Guild?.Name ?? "Unknown Guild";
        var status = $"Executing {type} from {location}:{channel}:@{context.User.Username}. {identity}";

        await Log(GetLog(LogSeverity.Info, status)).ConfigureAwait(false);
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
        var cfg = Hub.Config.Discord;
        if (!CommandsRegistered)
            await RegisterSlashCommands(cfg).ConfigureAwait(false);

        if (MessageChannelsLoaded)
            return;

        // Restore Echoes
        EchoModule.RestoreChannels(_client, cfg);

        // Restore Logging
        LogModule.RestoreLogging(_client, cfg);
        TradeStartModule<T>.RestoreTradeStarting(_client, cfg);

        // Don't let it load more than once in case of Discord hiccups.
        const string status = "Logging and Echo channels loaded!";
        await Log(GetLog(LogSeverity.Info, status)).ConfigureAwait(false);
        MessageChannelsLoaded = true;

        var game = cfg.BotGameStatus;
        if (!string.IsNullOrWhiteSpace(game))
            await _client.SetGameAsync(game).ConfigureAwait(false);
    }

    private async Task RegisterSlashCommands(DiscordSettings cfg)
    {
        var hash = ComputeSlashCommandHash();
        if (hash == cfg.SlashCommandHash)
        {
            LogUtil.LogInfo("Skipped registering interactions; no signature changes detected.");
            UpdateRegisteredCounts();
            return;
        }

        try
        {
            // deleteMissing=true removes obsolete application commands left behind by previous versions of the bot.
            // Global commands have a TTL of 1 hour
            await _interactions.RegisterCommandsGloballyAsync(deleteMissing: true).ConfigureAwait(false);
            CommandsRegistered = true;

            UpdateRegisteredCounts();
            cfg.SlashCommandHash = hash;

            var message = $"Registered interactions. Hash: {hash[..6]}";
            LogUtil.LogInfo(message);
        }
        catch (Exception ex)
        {
            LogUtil.LogSafe(ex);
        }
    }

    private void UpdateRegisteredCounts()
    {
        var commands = _interactions.SlashCommands.Count;
        var modals = _interactions.ModalCommands.Count;
        SysCordSettings.SetCommandsRegistered(commands, modals);
    }
}
