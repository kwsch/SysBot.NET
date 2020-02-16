using FluentAssertions;
using PKHeX.Core;
using SysBot.Pokemon;
using Xunit;

namespace SysBot.Tests
{
    public class PokePoolTests
    {
        [Fact]
        public void TestPool()
        {
            // Ensure that we can get more than one pokemon out of the pool.
            var pool = new PokemonPool<PK8>(new PokeTradeHubConfig());
            var a = new PK8 { Species = 5 };
            var b = new PK8 { Species = 12 };
            pool.Add(a);
            pool.Add(b);

            pool.Count.Should().BeGreaterOrEqualTo(2);

            while (true) { if (ReferenceEquals(pool.GetRandomPoke(), a)) break; }
            while (true) { if (ReferenceEquals(pool.GetRandomPoke(), b)) break; }
            while (true) { if (ReferenceEquals(pool.GetRandomPoke(), a)) break; }

            true.Should().BeTrue();
        }
    }
}
