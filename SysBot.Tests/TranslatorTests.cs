using FluentAssertions;
using PKHeX.Core;
using SysBot.Pokemon;
using SysBot.Pokemon.Helpers;
using System.Diagnostics;
using Xunit;

namespace SysBot.Tests
{
    public class TranslatorTests
    {
        static TranslatorTests() => AutoLegalityWrapper.EnsureInitialized(new LegalitySettings());

        [Theory]
        [InlineData("公肯泰罗携带大师球6V异色努力值252生命全招式异国-泰山压顶", "Tauros (M) @ Master Ball\nShiny: Yes\nIVs: 31 HP / 31 Atk / 31 Def / 31 SpA / 31 SpD / 31 Spe\nEVs: 252 HP \n.RelearnMoves=$suggestAll\nLanguage: Italian\n-Body Slam")]
        public void TestTrans(string input, string output)
        {
            var result = ShowdownTranslator<PK9>.Chinese2Showdown(input);
            result.Should().Be(output);
        }

        [Theory]
        [InlineData("皮卡丘")]
        [InlineData("木木枭")]
        [InlineData("彩粉蝶-冰雪花纹")]
        [InlineData("公小火龙的蛋")]
        [InlineData("大剑鬼")]
        [InlineData("火暴兽")]
        public void TestLegal(string input)
        {
            var setstring = ShowdownTranslator<PK9>.Chinese2Showdown(input);
            var set = ShowdownUtil.ConvertToShowdown(setstring);
            set.Should().NotBeNull();
            var template = AutoLegalityWrapper.GetTemplate(set);
            template.Species.Should().BeGreaterThan(0);
            var sav = AutoLegalityWrapper.GetTrainerInfo<PK9>();
            var pkm = sav.GetLegal(template, out var result);
            Trace.WriteLine(result.ToString());

            if (pkm.Nickname.ToLower() == "egg" && Breeding.CanHatchAsEgg(pkm.Species)) AbstractTrade<PK9>.EggTrade(pkm, template);

            pkm.CanBeTraded().Should().BeTrue();
            (pkm is PK9).Should().BeTrue();
            var la = new LegalityAnalysis(pkm);
            if (!la.Valid)
                Trace.WriteLine(la.Report());
            la.Valid.Should().BeTrue();
        }

    }

}
