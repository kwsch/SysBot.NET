using FluentAssertions;
using PKHeX.Core;
using SysBot.Pokemon;
using Xunit;

namespace SysBot.Tests
{
    public class GenerateTests
    {
        static GenerateTests() => AutoLegalityWrapper.EnsureInitialized(new PokeTradeHubConfig());

        [Theory]
        [InlineData(TorkoalH, 4)]
        public void TestAbility(string set, int abilNumber)
        {
            var sav = AutoLegalityWrapper.GetTrainerInfo(8);
            for (int i = 0; i < 10; i++)
            {
                var s = new ShowdownSet(set);
                var pk = sav.GetLegal(s, out _);
                pk.AbilityNumber.Should().Be(abilNumber);
            }
        }

        private const string TorkoalH =
            @"Torkoal (M) @ Assault Vest
IVs: 0 Atk
EVs: 248 HP / 8 Atk / 252 SpA
Ability: Drought
Quiet Nature
- Body Press
- Earth Power
- Eruption
- Fire Blast";
    }
}
