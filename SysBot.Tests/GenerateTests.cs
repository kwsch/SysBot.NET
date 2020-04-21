using FluentAssertions;
using PKHeX.Core;
using SysBot.Pokemon;
using Xunit;

namespace SysBot.Tests
{
    public class GenerateTests
    {
        static GenerateTests() => AutoLegalityWrapper.EnsureInitialized(new LegalitySettings());

        [Theory]
        [InlineData(Gengar)]
        [InlineData(Braviary)]
        [InlineData(Drednaw)]
        public void CanGenerate(string set)
        {
            var sav = AutoLegalityWrapper.GetTrainerInfo(8);
            var s = new ShowdownSet(set);
            var template = AutoLegalityWrapper.GetTemplate(s);
            var pk = sav.GetLegal(template, out _);
            pk.Should().NotBeNull();
        }

        [Theory]
        [InlineData(Torkoal2, 2)]
        [InlineData(Charizard4, 4)]
        public void TestAbility(string set, int abilNumber)
        {
            var sav = AutoLegalityWrapper.GetTrainerInfo(8);
            for (int i = 0; i < 10; i++)
            {
                var s = new ShowdownSet(set);
                var template = AutoLegalityWrapper.GetTemplate(s);
                var pk = sav.GetLegal(template, out _);
                pk.AbilityNumber.Should().Be(abilNumber);
            }
        }

        [Theory]
        [InlineData(Torkoal2, 2)]
        [InlineData(Charizard4, 4)]
        public void TestAbilityTwitch(string set, int abilNumber)
        {
            var sav = AutoLegalityWrapper.GetTrainerInfo(8);
            for (int i = 0; i < 10; i++)
            {
                var twitch = set.Replace("\r\n", " ").Replace("\n", " ");
                var s = TwitchShowdownUtil.ConvertToShowdown(twitch);
                var template = AutoLegalityWrapper.GetTemplate(s);
                var pk = sav.GetLegal(template, out _);
                pk.AbilityNumber.Should().Be(abilNumber);
            }
        }

        private const string Gengar =
@"Gengar-Gmax @ Life Orb 
Ability: Cursed Body 
Shiny: Yes 
EVs: 252 SpA / 4 SpD / 252 Spe 
Timid Nature 
- Dream Eater 
- Fling 
- Giga Impact 
- Headbutt";

        private const string Braviary =
@"Braviary (F) @ Master Ball
Ability: Defiant
EVs: 252 Atk / 4 SpD / 252 Spe
Jolly Nature
- Brave Bird
- Close Combat
- Tailwind
- Iron Head";

        private const string Drednaw =
@"Drednaw-Gmax @ Fossilized Drake 
Ability: Shell Armor 
Level: 60 
EVs: 252 Atk / 4 SpD / 252 Spe 
Adamant Nature 
- Earthquake 
- Liquidation 
- Swords Dance 
- Head Smash";

        private const string Torkoal2 =
@"Torkoal (M) @ Assault Vest
IVs: 0 Atk
EVs: 248 HP / 8 Atk / 252 SpA
Ability: Drought
Quiet Nature
- Body Press
- Earth Power
- Eruption
- Fire Blast";

        private const string Charizard4 =
@"Charizard @ Choice Scarf 
Ability: Solar Power 
Level: 50 
Shiny: Yes 
EVs: 252 SpA / 4 SpD / 252 Spe 
Timid Nature 
- Heat Wave 
- Air Slash 
- Solar Beam 
- Beat Up";
    }
}
