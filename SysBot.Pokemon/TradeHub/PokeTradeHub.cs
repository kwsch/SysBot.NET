using PKHeX.Core;
using SysBot.Base;
using System.Collections.Concurrent;

namespace SysBot.Pokemon
{
    /// <summary>
    /// Centralizes logic for trade bot coordination.
    /// </summary>
    /// <typeparam name="T">Type of <see cref="PKM"/> to distribute.</typeparam>
    public class PokeTradeHub<T> where T : PKM, new()
    {
        public PokeTradeHub(PokeTradeHubConfig config)
        {
            Config = config;
            var pool = new PokemonPool<T>(config);
            Ledy = new LedyDistributor<T>(pool);
            BotSync = new BotSynchronizer(config.Distribution);
            BotSync.BarrierReleasingActions.Add(() => LogUtil.LogInfo($"{BotSync.Barrier.ParticipantCount} bots released.", "Barrier"));

            Queues = new TradeQueueManager<T>(this);
        }

        public static readonly PokeTradeLogNotifier<T> LogNotifier = new();

        public readonly PokeTradeHubConfig Config;
        public readonly BotSynchronizer BotSync;

        /// <summary> Trade Bots only, used to delegate multi-player tasks </summary>
        public readonly ConcurrentPool<PokeRoutineExecutorBase> Bots = new();
        public bool TradeBotsReady => !Bots.All(z => z.Config.CurrentRoutineType == PokeRoutineType.Idle);
        public readonly TradeQueueManager<T> Queues;

        #region Distribution Queue
        public readonly LedyDistributor<T> Ledy;
        #endregion
    }
}
