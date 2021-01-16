using System;
using FluentAssertions;
using PKHeX.Core;
using SysBot.Pokemon;
using System.Threading;
using System.Threading.Tasks;
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

            var executor = new MockExecutor(new PokeBotState());

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
            first.Notifier.TradeFinished(executor, first, new PK8 { Species = 777 });

            var status = info.CheckPosition(t1.UserID, PokeRoutineType.LinkTrade);
            status.Position.Should().Be(1); // not zero indexed
            var count = info.UserCount(z => z.Type == PokeRoutineType.LinkTrade);
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
            count = info.UserCount(z => z.Type == PokeRoutineType.LinkTrade);
            count.Should().Be(2);
            queue.Count.Should().Be(2);
        }

        private class MockExecutor : PokeRoutineExecutor
        {
            public MockExecutor(PokeBotState cfg) : base(cfg) { }
            public override Task MainLoop(CancellationToken token) => Task.CompletedTask;
            public override void SoftStop() { }
        }

        private static TradeEntry<PK8> GetTestTrade(TradeQueueInfo<PK8> info, int tag, bool favor = false)
        {
            var trade = GetTestTrade(tag, favor);
            trade.Trade.Notifier.OnFinish = r => RemoveAndCheck(info, trade, r);
            return trade;
        }

        private static void RemoveAndCheck(TradeQueueInfo<PK8> info, TradeEntry<PK8> trade, PokeRoutineExecutor routine)
        {
            var result = info.Remove(trade);
            result.Should().BeTrue();
            routine.Should().NotBeNull();
        }

        private static TradeEntry<PK8> GetTestTrade(int tag, bool favor)
        {
            var d3 = new PokeTradeDetail<PK8>(new PK8 { Species = tag }, new PokeTradeTrainerInfo($"{(favor ? "*" : "")}Test {tag}"), new PokeTradeLogNotifier<PK8>(), PokeTradeType.Specific, tag, favor);
            return new TradeEntry<PK8>(d3, (ulong)tag, PokeRoutineType.LinkTrade, $"Test Trade {tag}");
        }

        [Fact]
        public void TestFavortism()
        {
            var settings = new PokeTradeHubConfig();
            var hub = new PokeTradeHub<PK8>(settings);
            var info = new TradeQueueInfo<PK8>(hub);
            var queue = info.Hub.Queues.GetQueue(PokeRoutineType.LinkTrade);

            const int count = 100;

            // Enqueue a bunch
            for (int i = 0; i < count; i++)
            {
                var s = GetTestTrade(info, i + 1);
                var r = info.AddToTradeQueue(s, s.UserID);
                r.Should().Be(QueueResultAdd.Added);
            }

            queue.Count.Should().Be(count);

            var f = settings.Favoritism;
            f.Mode = FavoredMode.Exponent;
            f.Multiply = 0.4f;
            f.Exponent = 0.777f;

            // Enqueue some favorites
            for (int i = 0; i < count / 10; i++)
            {
                var s = GetTestTrade(info, count + i + 1, true);
                var r = info.AddToTradeQueue(s, s.UserID);
                r.Should().Be(QueueResultAdd.Added);
            }

            int expectedPosition = (int)Math.Ceiling(Math.Pow(count, f.Exponent));
            for (int i = 0; i < expectedPosition; i++)
            {
                queue.TryDequeue(out var detail, out _);
                detail.IsFavored.Should().Be(false);
            }

            {
                queue.TryDequeue(out var detail, out _);
                detail.IsFavored.Should().Be(true);
            }
        }
    }
}