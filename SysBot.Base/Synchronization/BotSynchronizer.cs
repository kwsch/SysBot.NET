using System;
using System.Collections.Generic;
using System.Threading;

namespace SysBot.Base
{
    public class BotSynchronizer
    {
        private readonly ISynchronizationSetting Config;

        public BotSynchronizer(ISynchronizationSetting hub)
        {
            Config = hub;
            Barrier = new Barrier(0, ReleaseBarrier);
        }

        /// <summary>
        /// Blocks bots from proceeding until all participating bots are waiting at the same step.
        /// </summary>
        public readonly Barrier Barrier;

        public readonly List<Action> BarrierReleasingActions = new();

        /// <summary>
        /// When the Barrier releases the bots, this method is executed before the bots continue execution.
        /// </summary>
        private void ReleaseBarrier(Barrier b)
        {
            foreach (var action in BarrierReleasingActions)
                action();

            var ms = Config.SynchronizeDelayBarrier;
            if (ms != 0)
                Thread.Sleep(ms);
        }
    }
}