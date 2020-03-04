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
        public override string ToString() => "Queue Joining Settings";

        // General

        [Category(FeatureToggle), Description("Toggles if users can join the queue.")]
        public bool CanQueue { get; set; } = true;

        [Category(FeatureToggle), Description("Prevents adding users if there are this many users in the queue already.")]
        public int MaxQueueCount { get; set; } = 999;

        [Category(FeatureToggle), Description("Determines how Flex Mode will process the queues.")]
        public FlexYieldMode FlexMode { get; set; } = FlexYieldMode.Weighted;

        // Flex Users

        [Category(UserBias), Description("Biases the Trade Queue's weight based on how many users are in the queue.")]
        public int YieldMultCountTrade { get; set; } = 100;

        [Category(UserBias), Description("Biases the Dudu Queue's weight based on how many users are in the queue.")]
        public int YieldMultCountDudu { get; set; } = 100;

        [Category(UserBias), Description("Biases the Clone Queue's weight based on how many users are in the queue.")]
        public int YieldMultCountClone { get; set; } = 100;

        [Category(UserBias), Description("Biases the Dump Queue's weight based on how many users are in the queue.")]
        public int YieldMultCountDump { get; set; } = 100;

        // Flex Time

        [Category(TimeBias), Description("Determines whether the weight should be added or multiplied to the total weight.")]
        public FlexBiasMode YieldMultWait { get; set; } = FlexBiasMode.Multiply;

        [Category(TimeBias), Description("Checks time elapsed since the user joined the Trade queue, and increases the queue's weight accordingly.")]
        public int YieldMultWaitTrade { get; set; } = 1;

        [Category(TimeBias), Description("Checks time elapsed since the user joined the Dudu queue, and increases the queue's weight accordingly.")]
        public int YieldMultWaitDudu { get; set; } = 1;

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
                PokeTradeType.Dudu => YieldMultCountDudu,
                PokeTradeType.Clone => YieldMultCountClone,
                PokeTradeType.Dump => YieldMultCountDump,
                _ => YieldMultCountTrade
            };
        }

        private int GetTimeBias(PokeTradeType type)
        {
            return type switch
            {
                PokeTradeType.Dudu => YieldMultWaitDudu,
                PokeTradeType.Clone => YieldMultWaitClone,
                PokeTradeType.Dump => YieldMultWaitDump,
                _ => YieldMultWaitTrade
            };
        }

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
}