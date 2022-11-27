using PKHeX.Core;
using SysBot.Base;
using System.ComponentModel;

namespace SysBot.Pokemon
{
    public class DistributionSettings : ISynchronizationSetting
    {
        private const string Distribute = nameof(Distribute);
        private const string Synchronize = nameof(Synchronize);
        public override string ToString() => "Distribution Trade Settings";

        // Distribute

        [Category(Distribute), Description("When enabled, idle LinkTrade bots will randomly distribute PKM files from the DistributeFolder.")]
        public bool DistributeWhileIdle { get; set; } = true;

        [Category(Distribute), Description("When enabled, the DistributionFolder will yield randomly rather than in the same sequence.")]
        public bool Shuffled { get; set; }

        [Category(Distribute), Description("When set to something other than None, the Random Trades will require this species in addition to the nickname match.")]
        public Species LedySpecies { get; set; } = Species.None;

        [Category(Distribute), Description("When set to true, Random Ledy nickname-swap trades will quit rather than trade a random entity from the pool.")]
        public bool LedyQuitIfNoMatch { get; set; }

        [Category(Distribute), Description("Distribution Trade Link Code.")]
        public int TradeCode { get; set; } = 7196;

        [Category(Distribute), Description("Distribution Trade Link Code uses the Min and Max range rather than the fixed trade code.")]
        public bool RandomCode { get; set; }

        [Category(Distribute), Description("For BDSP, the distribution bot will go to a specific room and remain there until the bot is stopped.")]
        public bool RemainInUnionRoomBDSP { get; set; } = true;

        // Synchronize

        [Category(Synchronize), Description("Link Trade: Using multiple distribution bots -- all bots will confirm their trade code at the same time. When Local, the bots will continue when all are at the barrier. When Remote, something else must signal the bots to continue.")]
        public BotSyncOption SynchronizeBots { get; set; } = BotSyncOption.LocalSync;

        [Category(Synchronize), Description("Link Trade: Using multiple distribution bots -- once all bots are ready to confirm trade code, the Hub will wait X milliseconds before releasing all bots.")]
        public int SynchronizeDelayBarrier { get; set; }

        [Category(Synchronize), Description("Link Trade: Using multiple distribution bots -- how long (seconds) a bot will wait for synchronization before continuing anyways.")]
        public double SynchronizeTimeout { get; set; } = 90;

    }
}