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

            Twitch = new TwitchBot(Hub.Config.Twitch, Hub);
            Hub.BotSync.BarrierReleasingActions.Add(() => Twitch.StartingDistribution(config.MessageStart));
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