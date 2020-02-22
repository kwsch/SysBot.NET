using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using PKHeX.Core;
using SysBot.Pokemon;
using Xunit;

namespace SysBot.Tests
{
    public class QueueTests
    {
        [Fact]
        public void TestEnqueue()
        {
            var hub = new PokeTradeHub<PK8>(new PokeTradeHubConfig());
            var info = new TradeQueueInfo<PK8>(hub);
            var queue = info.Hub.Queues.GetQueue(PokeRoutineType.LinkTrade);

            var t1 = GetTestTrade(info, 1);
            var t2 = GetTestTrade(info, 2);
            var t3 = GetTestTrade(info, 3);
            var s = GetTestTrade(info, 4);

            var executor = new MockExecutor(new PokeBotConfig());

            // Enqueue a bunch
            var r1 = info.AddToTradeQueue(t1, t1.UserID);
            r1.Should().Be(QueueResultAdd.Added);

            var r2 = info.AddToTradeQueue(t2, t2.UserID);
            r2.Should().Be(QueueResultAdd.Added);

            var r3 = info.AddToTradeQueue(t3, t3.UserID);
            r3.Should().Be(QueueResultAdd.Added);

            // Sudo add with the same ID
            var id = t1.UserID;
            var sr = info.AddToTradeQueue(s, id);
            sr.Should().Be(QueueResultAdd.AlreadyInQueue);

            sr = info.AddToTradeQueue(s, id, true);
            sr.Should().Be(QueueResultAdd.Added);

            var dequeue = queue.TryDequeue(out var first, out uint priority);
            priority.Should().Be(PokeTradeQueue<PK8>.Tier1); // sudo
            dequeue.Should().BeTrue();
            ReferenceEquals(first, s.Trade).Should().BeTrue();

            first.Notifier.TradeInitialize(executor, first);
            first.Notifier.TradeSearching(executor, first);
            first.Notifier.TradeFinished(executor, first, new PK8 {Species = 777});

            var status = info.CheckPosition(t1.UserID, PokeRoutineType.LinkTrade);
            status.Position.Should().Be(1); // not zero indexed
            var count = info.Count(z => z.Type == PokeRoutineType.LinkTrade);
            count.Should().Be(3);
            queue.Count.Should().Be(3);

            dequeue = queue.TryDequeue(out var second, out priority);
            priority.Should().Be(PokeTradeQueue<PK8>.TierFree); // sudo
            dequeue.Should().BeTrue();
            ReferenceEquals(second, t1.Trade).Should().BeTrue();

            second.Notifier.TradeInitialize(executor, second);
            second.Notifier.TradeSearching(executor, second);
            second.Notifier.TradeCanceled(executor, second, PokeTradeResult.TrainerTooSlow);

            status = info.CheckPosition(t1.UserID, PokeRoutineType.LinkTrade);
            status.Position.Should().Be(-1);
            count = info.Count(z => z.Type == PokeRoutineType.LinkTrade);
            count.Should().Be(2);
            queue.Count.Should().Be(2);
        }

        private class MockExecutor : PokeRoutineExecutor
        {
            public MockExecutor(PokeBotConfig cfg) : base(cfg) { }
            protected override Task MainLoop(CancellationToken token) => Task.CompletedTask;
        }

        private static TradeEntry<PK8> GetTestTrade(TradeQueueInfo<PK8> info, int tag)
        {
            var trade = GetTestTrade(tag);
            trade.Trade.Notifier.OnFinish = r => RemoveAndCheck(info, trade, r);
            return trade;
        }

        private static void RemoveAndCheck(TradeQueueInfo<PK8> info, TradeEntry<PK8> trade, PokeRoutineExecutor routine)
        {
            var result = info.Remove(trade);
            result.Should().BeTrue();
            routine.Should().NotBeNull();
        }

        private static TradeEntry<PK8> GetTestTrade(int tag)
        {
            var d3 = new PokeTradeDetail<PK8>(new PK8 {Species = tag}, new PokeTradeTrainerInfo($"Test {tag}"), new PokeTradeLogNotifier<PK8>(), PokeTradeType.Specific, tag);
            return new TradeEntry<PK8>(d3, (ulong)tag, PokeRoutineType.LinkTrade, $"Test Trade {tag}");
        }
    }
}