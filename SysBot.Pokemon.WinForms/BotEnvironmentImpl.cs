using System.Threading;
using System.Threading.Tasks;
using PKHeX.Core;
using SysBot.Pokemon.Discord;

namespace SysBot.Pokemon
{
    /// <summary>
    /// Bot Environment implementation with Integrations added.
    /// </summary>
    public class BotEnvironmentImpl : BotEnvironment
    {
        public BotEnvironmentImpl(PokeTradeHub<PK8> hub) : base(hub) { }
        public BotEnvironmentImpl(PokeTradeHubConfig config) : base(config) { }

        protected override void AddIntegrations()
        {
            if (!string.IsNullOrWhiteSpace(Hub.Config.DiscordToken))
                AddDiscordBot(Hub.Config.DiscordToken);
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