using PKHeX.Core;
using SysBot.Pokemon.Discord;
using SysBot.Pokemon.Twitch;
using System.Threading;
using System.Threading.Tasks;

namespace SysBot.Pokemon.ConsoleApp
{
    /// <summary>
    /// Bot Environment implementation with Integrations added.
    /// </summary>
    public class PokeBotRunnerImpl<T> : PokeBotRunner<T> where T : PKM, new()
    {
        public PokeBotRunnerImpl(PokeTradeHub<T> hub, BotFactory<T> fac) : base(hub, fac) { }
        public PokeBotRunnerImpl(PokeTradeHubConfig config, BotFactory<T> fac) : base(config, fac) { }

        private static TwitchBot<T>? Twitch;

        protected override void AddIntegrations()
        {
            if (!string.IsNullOrWhiteSpace(Hub.Config.Discord.Token))
                AddDiscordBot(Hub.Config.Discord.Token);

            if (!string.IsNullOrWhiteSpace(Hub.Config.Twitch.Token))
                AddTwitchBot(Hub.Config.Twitch);
        }

        private void AddTwitchBot(TwitchSettings config)
        {
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

        private void AddDiscordBot(string apiToken)
        {
            SysCordInstance<T>.Runner = this;
            var bot = new SysCord<T>(Hub);
            Task.Run(() => bot.MainAsync(apiToken, CancellationToken.None));
        }
    }
}