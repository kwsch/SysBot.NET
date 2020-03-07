using System.Collections.Generic;
using System.Threading;

namespace SysBot.Pokemon
{
    public class BotCompleteCounts
    {
        private readonly CountSettings Config;

        private int CompletedTrades;
        private int CompletedEggs;
        private int CompletedFossils;
        private int CompletedEncounters;
        private int CompletedDudu;
        private int CompletedSurprise;
        private int CompletedDistribution;
        private int CompletedClones;
        private int CompletedDumps;
        private int CompletedRaids;

        public BotCompleteCounts(CountSettings config)
        {
            Config = config;
            ReloadCounts();
        }

        public void ReloadCounts()
        {
            CompletedTrades = Config.CompletedTrades;
            CompletedEggs = Config.CompletedEggs;
            CompletedDudu = Config.CompletedDudu;
            CompletedFossils = Config.CompletedFossils;
            CompletedEncounters = Config.CompletedEncounters;
            CompletedSurprise = Config.CompletedSurprise;
            CompletedDistribution = Config.CompletedDistribution;
            CompletedClones = Config.CompletedClones;
            CompletedDumps = Config.CompletedDumps;
            CompletedRaids = Config.CompletedRaids;
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

        public void AddCompletedFossils()
        {
            Interlocked.Increment(ref CompletedFossils);
            Config.CompletedFossils = CompletedFossils;
        }
        
        public void AddCompletedEncounters()
        {
            Interlocked.Increment(ref CompletedEncounters);
            Config.CompletedEncounters = CompletedEncounters;
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

        public void AddCompletedRaids()
        {
            Interlocked.Increment(ref CompletedRaids);
            Config.CompletedRaids = CompletedRaids;
        }

        public void AddCompletedDumps()
        {
            Interlocked.Increment(ref CompletedDumps);
            Config.CompletedDumps = CompletedDumps;
        }

        public IEnumerable<string> Summary()
        {
            if (CompletedDudu != 0)
                yield return $"Dudu Trades: {CompletedDudu}";
            if (CompletedClones != 0)
                yield return $"Clone Trades: {CompletedClones}";
            if (CompletedDumps != 0)
                yield return $"Dump Trades: {CompletedDumps}";
            if (CompletedTrades != 0)
                yield return $"Link Trades: {CompletedTrades}";
            if (CompletedDistribution != 0)
                yield return $"Distribution Trades: {CompletedDistribution}";
            if (CompletedSurprise != 0)
                yield return $"Surprise Trades: {CompletedSurprise}";
            if (CompletedEggs != 0)
                yield return $"Eggs Received: {CompletedEggs}";
            if (CompletedRaids != 0)
                yield return $"Completed Raids: {CompletedRaids}";
            if (CompletedFossils != 0)
                yield return $"Completed Raids: {CompletedFossils}";
        }
    }
}