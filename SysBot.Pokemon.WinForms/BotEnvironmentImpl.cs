using System.Threading;
using System.Threading.Tasks;
using PKHeX.Core;
using SysBot.Pokemon.Discord;
using SysBot.Pokemon.Twitch;

namespace SysBot.Pokemon
{
    /// <summary>
    /// Bot Environment implementation with Integrations added.
    /// </summary>
    public class BotEnvironmentImpl : BotEnvironment
    {
        public BotEnvironmentImpl(PokeTradeHub<PK8> hub) : base(hub) { }
        public BotEnvironmentImpl(PokeTradeHubConfig config) : base(config) { }

        private TwitchBot? Twitch;

        protected override void AddIntegrations()
        {
            if (!string.IsNullOrWhiteSpace(Hub.Config.DiscordToken))
                AddDiscordBot(Hub.Config.DiscordToken);

            if (!string.IsNullOrWhiteSpace(Hub.Config.TwitchToken))
                AddTwitchBot(Hub.Config);
        }

        private void AddTwitchBot(ITwitchSettings config)
        {
            if (Twitch != null)
                return; // already created

            if (string.IsNullOrWhiteSpace(config.TwitchChannel))
                return;
            if (string.IsNullOrWhiteSpace(config.TwitchUsername))
                return;
            if (string.IsNullOrWhiteSpace(config.TwitchToken))
                return;

            Twitch = new TwitchBot(config.TwitchUsername, config.TwitchToken, config.TwitchChannel);
            Hub.BotSync.BarrierReleasingActions.Add(() => Twitch.StartingDistribution(config.TwitchMessageStart));
        }

        private void AddDiscordBot(string apiToken)
        {
            if (SysCordInstance.Self != null)
            {
                SysCordInstance.Self.Hub = Hub;
                return;
            }
            var bot = new SysCord(Hub);
            Task.Run(() => bot.MainAsync(apiToken, CancellationToken.None));
        }
    }
}