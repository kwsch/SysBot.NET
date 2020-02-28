using FluentAssertions;
using SysBot.Pokemon;
using Xunit;

namespace SysBot.Tests
{
    public class StringTests
    {
        [Theory]
        [InlineData("Anubis", "anubis")]
        [InlineData("Anu_bis", "anubis")]
        [InlineData("Anu_bis_12", "anubis12")]
        [InlineData("a-zA-Z0-9ａ-ｚＡ-Ｚ０-９", "azaz09azaz09")]
        public void TestSanitize(string input, string output)
        {
            var result = StringsUtil.Sanitize(input);
            result.Should().Be(output);
        }
    }
}