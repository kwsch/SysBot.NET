﻿using System.Collections.Generic;
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
        private int CompletedLegends;
        private int CompletedSeedChecks;
        private int CompletedSurprise;
        private int CompletedDistribution;
        private int CompletedClones;
        private int CompletedFixOTs;
        private int CompletedDumps;
        private int CompletedRaids;

        public BotCompleteCounts(CountSettings config)
        {
            Config = config;
            LoadCountsFromConfig();
        }

        public void LoadCountsFromConfig()
        {
            CompletedTrades = Config.CompletedTrades;
            CompletedEggs = Config.CompletedEggs;
            CompletedFossils = Config.CompletedFossils;
            CompletedEncounters = Config.CompletedEncounters;
            CompletedLegends = Config.CompletedLegends;
            CompletedSeedChecks = Config.CompletedSeedChecks;
            CompletedSurprise = Config.CompletedSurprise;
            CompletedDistribution = Config.CompletedDistribution;
            CompletedClones = Config.CompletedClones;
            CompletedFixOTs = Config.CompletedFixOTs;
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
        public void AddCompletedLegends()
        {
            Interlocked.Increment(ref CompletedLegends);
            Config.CompletedLegends = CompletedLegends;
        }

        public void AddCompletedSeedCheck()
        {
            Interlocked.Increment(ref CompletedSeedChecks);
            Config.CompletedSeedChecks = CompletedSeedChecks;
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

        public void AddCompletedFixOTs()
        {
            Interlocked.Increment(ref CompletedFixOTs);
            Config.CompletedFixOTs = CompletedFixOTs;
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
            if (CompletedSeedChecks != 0)
                yield return $"Seed Check Trades: {CompletedSeedChecks}";
            if (CompletedClones != 0)
                yield return $"Clone Trades: {CompletedClones}";
            if (CompletedFixOTs != 0)
                yield return $"FixOT Trades: {CompletedFixOTs}";
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
                yield return $"Completed Fossils: {CompletedFossils}";
            if (CompletedEncounters != 0)
                yield return $"Wild Encounters: {CompletedEncounters}";
            if (CompletedLegends != 0)
                yield return $"Legendary Encounters: {CompletedLegends}";
        }
    }
}
