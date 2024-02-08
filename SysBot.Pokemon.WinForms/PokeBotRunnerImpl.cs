using PKHeX.Core;
using SysBot.Pokemon.Discord;
using SysBot.Pokemon.Twitch;
using SysBot.Pokemon.WinForms;
using SysBot.Pokemon.YouTube;
using System.Threading;
using System.Threading.Tasks;
using SysBot.Pokemon.Bilibili;
using SysBot.Pokemon.Dodo;
using SysBot.Pokemon.QQ;

namespace SysBot.Pokemon;

/// <summary>
/// Bot Environment implementation with Integrations added.
/// </summary>
public class PokeBotRunnerImpl<T> : PokeBotRunner<T> where T : PKM, new()
{
    public PokeBotRunnerImpl(PokeTradeHub<T> hub, BotFactory<T> fac) : base(hub, fac) { }
    public PokeBotRunnerImpl(PokeTradeHubConfig config, BotFactory<T> fac) : base(config, fac) { }

    private TwitchBot<T>? Twitch;
    private YouTubeBot<T>? YouTube;
    private MiraiQQBot<T>? QQ;
    private BilibiliLiveBot<T>? Bilibili;
    private DodoBot<T>? Dodo;

    protected override void AddIntegrations()
    {
        AddDiscordBot(Hub.Config.Discord.Token);
        AddTwitchBot(Hub.Config.Twitch);
        AddYouTubeBot(Hub.Config.YouTube);
        AddQQBot(Hub.Config.QQ);
        AddBilibiliBot(Hub.Config.Bilibili);
        AddDodoBot(Hub.Config.Dodo);
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

        Twitch = new TwitchBot<T>(Hub.Config.Twitch, Hub);
        if (Hub.Config.Twitch.DistributionCountDown)
            Hub.BotSync.BarrierReleasingActions.Add(() => Twitch.StartingDistribution(config.MessageStart));
    }

    private void AddYouTubeBot(YouTubeSettings config)
    {
        if (string.IsNullOrWhiteSpace(config.ClientID))
            return;
        if (YouTube != null)
            return; // already created

        WinFormsUtil.Alert("Please Login with your Browser");
        if (string.IsNullOrWhiteSpace(config.ChannelID))
            return;
        if (string.IsNullOrWhiteSpace(config.ClientID))
            return;
        if (string.IsNullOrWhiteSpace(config.ClientSecret))
            return;

        YouTube = new YouTubeBot<T>(Hub.Config.YouTube, Hub);
        Hub.BotSync.BarrierReleasingActions.Add(() => YouTube.StartingDistribution(config.MessageStart));
    }

    private void AddDiscordBot(string apiToken)
    {
        if (string.IsNullOrWhiteSpace(apiToken))
            return;
        var bot = new SysCord<T>(this);
        Task.Run(() => bot.MainAsync(apiToken, CancellationToken.None));
    }

    private void AddQQBot(QQSettings config)
    {
        if (string.IsNullOrWhiteSpace(config.VerifyKey) || string.IsNullOrWhiteSpace(config.Address)) return;
        if (string.IsNullOrWhiteSpace(config.QQ) || string.IsNullOrWhiteSpace(config.GroupId)) return;
        if (QQ != null) return;
        //add qq bot
        QQ = new MiraiQQBot<T>(config, Hub);
    }

    private void AddBilibiliBot(BilibiliSettings config)
    {
        if (string.IsNullOrWhiteSpace(config.LogUrl) || config.RoomId == 0) return;
        if (Bilibili != null) return;
        Bilibili = new BilibiliLiveBot<T>(config, Hub);
    }

    private void AddDodoBot(DodoSettings config)
    {
        if (string.IsNullOrWhiteSpace(config.BaseApi) || string.IsNullOrWhiteSpace(config.ClientId) || string.IsNullOrWhiteSpace(config.Token)) return;
        if (Dodo != null) return;
        Dodo = new DodoBot<T>(config, Hub);
    }
}