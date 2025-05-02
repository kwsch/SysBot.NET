using PKHeX.Core;
using SysBot.Pokemon.Discord;
using SysBot.Pokemon.Twitch;
using System.Threading;
using System.Threading.Tasks;

namespace SysBot.Pokemon.ConsoleApp;

/// <summary>
/// Bot Environment implementation with Integrations added.
/// </summary>
public class PokeBotRunnerImpl<T> : PokeBotRunner<T> where T : PKM, new()
{
    private static TwitchBot<T>? Twitch;
    private readonly ProgramConfig _config;

    public PokeBotRunnerImpl(PokeTradeHub<T> hub, BotFactory<T> fac, ProgramConfig config) : base(hub, fac)
    {
        _config = config;
    }

    protected override void AddIntegrations()
    {
        AddDiscordBot(Hub.Config.Discord);
        AddTwitchBot(Hub.Config.Twitch);
    }

    private void AddDiscordBot(DiscordSettings config)
    {
        var token = config.Token;
        if (string.IsNullOrWhiteSpace(token))
            return;

        var bot = new SysCord<T>(this, _config);
        Task.Run(() => bot.MainAsync(token, CancellationToken.None), CancellationToken.None);
    }

    private void AddTwitchBot(TwitchSettings config)
    {
        if (string.IsNullOrWhiteSpace(config.Token))
            return;
        if (Twitch != null)
            return; // already created

        if (string.IsNullOrWhiteSpace(config.Channel))
            return;
        if (string.IsNullOrWhiteSpace(config.Username))
            return;
        if (string.IsNullOrWhiteSpace(config.Token))
            return;

        Twitch = new TwitchBot<T>(config, Hub);
        if (config.DistributionCountDown)
            Hub.BotSync.BarrierReleasingActions.Add(() => Twitch.StartingDistribution(config.MessageStart));
    }
}
