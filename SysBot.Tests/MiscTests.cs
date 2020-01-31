using FluentAssertions;
using SysBot.Pokemon;
using Xunit;

namespace SysBot.Tests
{
    public class MiscTests
    {
        [Theory]
        [InlineData(8111, 0, 8)]
        [InlineData(1234, 0, 1)]
        [InlineData(1234, 1, 2)]
        [InlineData(1234, 2, 3)]
        [InlineData(1234, 3, 4)]
        public void Test(int code, int digit, int expect)
        {
            var result = TradeUtil.GetCodeDigit(code, digit);
            result.Should().Be(expect);
        }
    }
}
