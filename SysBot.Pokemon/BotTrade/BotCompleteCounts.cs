using System.Threading;

namespace SysBot.Pokemon
{
    public class BotCompleteCounts
    {
        private readonly PokeTradeHubConfig Config;

        private int CompletedTrades;
        private int CompletedEggs;
        private int CompletedDudu;
        private int CompletedSurprise;
        private int CompletedDistribution;
        private int CompletedClones;

        public BotCompleteCounts(PokeTradeHubConfig config)
        {
            Config = config;
            ReloadCounts();
        }

        public void ReloadCounts()
        {
            CompletedTrades = Config.CompletedTrades;
            CompletedEggs = Config.CompletedEggs;
            CompletedDudu = Config.CompletedDudu;
            CompletedSurprise = Config.CompletedSurprise;
            CompletedDistribution = Config.CompletedDistribution;
            CompletedClones = Config.CompletedClones;
        }

        public void AddCompletedTrade()
        {
            Interlocked.Increment(ref CompletedTrades);
            Config.CompletedTrades = CompletedTrades;
        }

        public void AddCompletedEggs()
        {
            Interlocked.Increment(ref CompletedEggs);
            Config.CompletedEggs = CompletedEggs;
        }

        public void AddCompletedDudu()
        {
            Interlocked.Increment(ref CompletedDudu);
            Config.CompletedDudu = CompletedDudu;
        }

        public void AddCompletedSurprise()
        {
            Interlocked.Increment(ref CompletedSurprise);
            Config.CompletedSurprise = CompletedSurprise;
        }

        public void AddCompletedDistribution()
        {
            Interlocked.Increment(ref CompletedDistribution);
            Config.CompletedDistribution = CompletedDistribution;
        }

        public void AddCompletedClones()
        {
            Interlocked.Increment(ref CompletedClones);
            Config.CompletedClones = CompletedClones;
        }
    }
}