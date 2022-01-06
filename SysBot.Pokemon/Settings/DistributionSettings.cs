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

        [Category(Distribute), Description("When enabled, idle LinkTrade bots will randomly distribute or Surprise Trade PKM files from the DistributeFolder.\r\n BDSP Bot can only Distribute.")]
        public bool DoSomethingWhileIdle { get; set; } = true;

        [Category(Distribute), Description("[SWSH only] Should the Bot do Surprise Trades or Distribution?")]
        public DistOrSurprise DistributeOrSurprise { get; set; }

        [Category(Distribute), Description("When enabled, the DistributionFolder will yield randomly rather than in the same sequence.")]
        public bool Shuffled { get; set; }

        [Category(Distribute), Description("When set to something other than None, the Random Trades will require this species in addition to the nickname match.")]
        public Species LedySpecies { get; set; } = Species.Wooloo;

        [Category(Distribute), Description("When set to true, Random Ledy nickname-swap trades will quit rather than trade a random entity from the pool.")]
        public bool LedyQuitIfNoMatch { get; set; }

        [Category(Distribute), Description("Distribution Trade Link Code.")]
        public int TradeCode { get; set; } = 7196;

        [Category(Distribute), Description("Distribution Trade Link Code uses the Min and Max range rather than the fixed trade code.")]
        public bool RandomCode { get; set; }



        // Synchronize

        [Category(Synchronize), Description("Link Trade: Using multiple distribution bots -- all bots will confirm their trade code at the same time. When Local, the bots will continue when all are at the barrier. When Remote, something else must signal the bots to continue.")]
        public BotSyncOption SynchronizeBots { get; set; } = BotSyncOption.LocalSync;

        [Category(Synchronize), Description("Link Trade: Using multiple distribution bots -- once all bots are ready to confirm trade code, the Hub will wait X milliseconds before releasing all bots.")]
        public int SynchronizeDelayBarrier { get; set; }

        [Category(Synchronize), Description("Link Trade: Using multiple distribution bots -- how long (Seconds) a bot will wait for synchronization before continuing anyways.")]
        public double SynchronizeTimeout { get; set; } = 90;
    }

    public enum DistOrSurprise
    {
        SurpriseTrade = 1,            
        Distribution = 2           
    }
}