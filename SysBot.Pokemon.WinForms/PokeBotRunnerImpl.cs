using PKHeX.Core;
using SysBot.Pokemon.Discord;
using SysBot.Pokemon.Twitch;
using SysBot.Pokemon.YouTube;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SysBot.Pokemon
{
    /// <summary>
    /// Bot Environment implementation with Integrations added.
    /// </summary>
    public class PokeBotRunnerImpl : PokeBotRunner
    {
        public PokeBotRunnerImpl(PokeTradeHub<PK8> hub) : base(hub) { }
        public PokeBotRunnerImpl(PokeTradeHubConfig config) : base(config) { }

        private static TwitchBot? Twitch;
        private static YouTubeBot? YouTube;

        protected override void AddIntegrations()
        {
            if (!string.IsNullOrWhiteSpace(Hub.Config.Discord.Token))
                AddDiscordBot(Hub.Config.Discord.Token);

            if (!string.IsNullOrWhiteSpace(Hub.Config.Twitch.Token))
                AddTwitchBot(Hub.Config.Twitch);

            if (!string.IsNullOrWhiteSpace(Hub.Config.YouTube.ClientID))
                AddYouTubeBot(Hub.Config.YouTube);
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

        private void AddYouTubeBot(YouTubeSettings config)
        {
            if (YouTube != null)
                return; // already created

            MessageBox.Show("Please Login in your Browser");
            if (string.IsNullOrWhiteSpace(config.ChannelID))
                return;
            if (string.IsNullOrWhiteSpace(config.ClientID))
                return;
            if (string.IsNullOrWhiteSpace(config.ClientSecret))
                return;

            YouTube = new YouTubeBot(Hub.Config.YouTube, Hub);
            Hub.BotSync.BarrierReleasingActions.Add(() => YouTube.StartingDistribution(config.MessageStart));
        }

        private void AddDiscordBot(string apiToken)
        {
            SysCordInstance.Runner = this;
            var bot = new SysCord(Hub);
            Task.Run(() => bot.MainAsync(apiToken, CancellationToken.None));
        }
    }
}