using PKHeX.Core;
using SysBot.Pokemon.Discord;
using System.Threading;
using System.Threading.Tasks;


namespace SysBot.Pokemon.ConsoleApp;

/// <summary>
/// Bot Environment implementation with Integrations added.
/// </summary>
public class PokeBotRunnerImpl<T> : PokeBotRunner<T> where T : PKM, new()
{
    private readonly ProgramConfig _config;
    public PokeBotRunnerImpl(PokeTradeHub<T> hub, BotFactory<T> fac) : base(hub, fac) { }
    public PokeBotRunnerImpl(PokeTradeHubConfig config, BotFactory<T> fac) : base(config, fac) { }



    protected override void AddIntegrations()
    {
        AddDiscordBot(Hub.Config.Discord);
    }

    private void AddDiscordBot(DiscordSettings config)
    {
        var token = config.Token;
        if (string.IsNullOrWhiteSpace(token))
            return;

        var programConfig = new ProgramConfig
        {
            Discord = config
        };

        var bot = new SysCord<T>(this, programConfig);  // Pass ProgramConfig here
        Task.Run(() => bot.MainAsync(token, CancellationToken.None), CancellationToken.None);
    }

}
