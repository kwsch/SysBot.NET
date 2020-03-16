using System.Threading;
using System.Threading.Tasks;
using PKHeX.Core;
using SysBot.Base;

namespace SysBot.Pokemon
{
    public class QueueMonitor
    {
        private readonly PokeTradeHub<PK8> Hub;
        public QueueMonitor(PokeTradeHub<PK8> hub) => Hub = hub;

        public async Task MonitorOpenQueue(CancellationToken token)
        {
            var queues = Hub.Queues.Info;
            var settings = Hub.Config.Queues;
            float secWaited = 0;

            const int sleepDelay = 0_500;
            const float sleepSeconds = sleepDelay / 1000f;
            while (!token.IsCancellationRequested)
            {
                await Task.Delay(sleepDelay, token).ConfigureAwait(false);
                var mode = settings.QueueToggleMode;
                if (!UpdateCanQueue(mode, settings, queues, secWaited))
                {
                    secWaited += sleepSeconds;
                    continue;
                }

                secWaited = 0;
                var state = queues.GetCanQueue()
                    ? "Users are now able to join the trade queue."
                    : "Changed queue settings: **Users CANNOT join the queue until it is turned back on.**";
                EchoUtil.Echo(state);
            }
        }

        private static bool UpdateCanQueue(QueueOpening mode, QueueSettings settings, TradeQueueInfo<PK8> queues, float secWaited)
        {
            return mode switch
            {
                QueueOpening.Threshold => CheckThreshold(settings, queues),
                QueueOpening.Interval => CheckInterval(settings, queues, secWaited),
                _ => false
            };
        }

        private static bool CheckInterval(QueueSettings settings, TradeQueueInfo<PK8> queues, float secWaited)
        {
            if (settings.CanQueue)
            {
                if (secWaited >= settings.IntervalOpenFor)
                    queues.ToggleQueue();
                else
                    return false;
            }
            else
            {
                if (secWaited >= settings.IntervalCloseFor)
                    queues.ToggleQueue();
                else
                    return false;
            }

            return true;
        }

        private static bool CheckThreshold(QueueSettings settings, TradeQueueInfo<PK8> queues)
        {
            if (settings.CanQueue)
            {
                if (queues.Count >= settings.ThresholdLock)
                    queues.ToggleQueue();
                else
                    return false;
            }
            else
            {
                if (queues.Count <= settings.ThresholdUnlock)
                    queues.ToggleQueue();
                else
                    return false;
            }

            return true;
        }
    }
}