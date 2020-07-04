using System;
using System.ComponentModel;
// ReSharper disable AutoPropertyCanBeMadeGetOnly.Global

namespace SysBot.Pokemon
{
    public class QueueSettings
    {
        private const string FeatureToggle = nameof(FeatureToggle);
        private const string UserBias = nameof(UserBias);
        private const string TimeBias = nameof(TimeBias);
        private const string QueueToggle = nameof(QueueToggle);
        public override string ToString() => "Queue Joining Settings";

        // General

        [Category(FeatureToggle), Description("Toggles if users can join the queue.")]
        public bool CanQueue { get; set; } = true;

        [Category(FeatureToggle), Description("Prevents adding users if there are this many users in the queue already.")]
        public int MaxQueueCount { get; set; } = 999;

        [Category(FeatureToggle), Description("Determines how Flex Mode will process the queues.")]
        public FlexYieldMode FlexMode { get; set; } = FlexYieldMode.Weighted;

        [Category(FeatureToggle), Description("Determines when the queue is turned on and off.")]
        public QueueOpening QueueToggleMode { get; set; } = QueueOpening.Threshold;

        // Queue Toggle

        [Category(QueueToggle), Description("Threshold Mode: Count of users that will cause the queue to open.")]
        public int ThresholdUnlock { get; set; } = 0;

        [Category(QueueToggle), Description("Threshold Mode: Count of users that will cause the queue to close.")]
        public int ThresholdLock { get; set; } = 30;

        [Category(QueueToggle), Description("Scheduled Mode: Seconds of being open before the queue locks.")]
        public int IntervalOpenFor { get; set; } = 5 * 60;

        [Category(QueueToggle), Description("Scheduled Mode: Seconds of being closed before the queue unlocks.")]
        public int IntervalCloseFor { get; set; } = 15 * 60;

        // Flex Users

        [Category(UserBias), Description("Biases the Trade Queue's weight based on how many users are in the queue.")]
        public int YieldMultCountTrade { get; set; } = 100;

        [Category(UserBias), Description("Biases the Seed Check Queue's weight based on how many users are in the queue.")]
        public int YieldMultCountSeedCheck { get; set; } = 100;

        [Category(UserBias), Description("Biases the Clone Queue's weight based on how many users are in the queue.")]
        public int YieldMultCountClone { get; set; } = 100;

        [Category(UserBias), Description("Biases the Dump Queue's weight based on how many users are in the queue.")]
        public int YieldMultCountDump { get; set; } = 100;

        // Flex Time

        [Category(TimeBias), Description("Determines whether the weight should be added or multiplied to the total weight.")]
        public FlexBiasMode YieldMultWait { get; set; } = FlexBiasMode.Multiply;

        [Category(TimeBias), Description("Checks time elapsed since the user joined the Trade queue, and increases the queue's weight accordingly.")]
        public int YieldMultWaitTrade { get; set; } = 1;

        [Category(TimeBias), Description("Checks time elapsed since the user joined the Seed Check queue, and increases the queue's weight accordingly.")]
        public int YieldMultWaitSeedCheck { get; set; } = 1;

        [Category(TimeBias), Description("Checks time elapsed since the user joined the Clone queue, and increases the queue's weight accordingly.")]
        public int YieldMultWaitClone { get; set; } = 1;

        [Category(TimeBias), Description("Checks time elapsed since the user joined the Dump queue, and increases the queue's weight accordingly.")]
        public int YieldMultWaitDump { get; set; } = 1;

        [Category(TimeBias), Description("Multiplies the amount of users in queue to give an estimate of how much time it will take until the user is processed.")]
        public float EstimatedDelayFactor { get; set; } = 1.1f;

        private int GetCountBias(PokeTradeType type)
        {
            return type switch
            {
                PokeTradeType.Seed => YieldMultCountSeedCheck,
                PokeTradeType.Clone => YieldMultCountClone,
                PokeTradeType.Dump => YieldMultCountDump,
                _ => YieldMultCountTrade
            };
        }

        private int GetTimeBias(PokeTradeType type)
        {
            return type switch
            {
                PokeTradeType.Seed => YieldMultWaitSeedCheck,
                PokeTradeType.Clone => YieldMultWaitClone,
                PokeTradeType.Dump => YieldMultWaitDump,
                _ => YieldMultWaitTrade
            };
        }

        /// <summary>
        /// Gets the weight of a <see cref="PokeTradeType"/> based on the count of users in the queue and time users have waited.
        /// </summary>
        /// <param name="count">Count of users for <see cref="type"/></param>
        /// <param name="time">Next-to-be-processed user's time joining the queue</param>
        /// <param name="type">Queue type</param>
        /// <returns>Effective weight for the trade type.</returns>
        public long GetWeight(int count, DateTime time, PokeTradeType type)
        {
            var now = DateTime.Now;
            var seconds = (now - time).Seconds;

            var cb = GetCountBias(type) * count;
            var tb = GetTimeBias(type) * seconds;

            if (YieldMultWait == FlexBiasMode.Multiply)
                return cb * tb;
            return cb + tb;
        }

        /// <summary>
        /// Estimates the amount of time (minutes) until the user will be processed.
        /// </summary>
        /// <param name="position">Position in the queue</param>
        /// <param name="botct">Amount of bots processing requests</param>
        /// <returns>Estimated time in Minutes</returns>
        public float EstimateDelay(int position, int botct) => (EstimatedDelayFactor * position) / botct;
    }

    public enum FlexBiasMode
    {
        Add,
        Multiply,
    }

    public enum FlexYieldMode
    {
        LessCheatyFirst,
        Weighted,
    }

    public enum QueueOpening
    {
        Manual,
        Threshold,
        Interval,
    }
}