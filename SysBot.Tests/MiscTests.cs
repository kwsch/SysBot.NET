using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using SysBot.Base;
using SysBot.Pokemon;
using Xunit;
using static SysBot.Base.SwitchButton;

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
        public void TestDigit(int code, int digit, int expect)
        {
            var result = TradeUtil.GetCodeDigit(code, digit);
            result.Should().Be(expect);
        }

        [Theory]
        [InlineData(1111, new[] { A, A, A, A })]
        [InlineData(0000, new[] { DDOWN, DDOWN, DDOWN, A, A, A, A })]
        [InlineData(1234, new[] { A, DRIGHT, A, DRIGHT, A, DDOWN, DLEFT, DLEFT, A })]
        [InlineData(0006, new[] { DDOWN, DDOWN, DDOWN, A, A, A, DUP, DUP, DRIGHT, A })]
        [InlineData(0103, new[] { DDOWN, DDOWN, DDOWN, A, DUP, DUP, DUP, DLEFT, A, DDOWN, DDOWN, DDOWN, A, DUP, DUP, DUP, DRIGHT, A })]
        [InlineData(9137, new[] { DDOWN, DDOWN, DRIGHT, DRIGHT, A, DUP, DUP, DLEFT, DLEFT, A, DRIGHT, DRIGHT, A, DDOWN, DDOWN, DLEFT, DLEFT, A })]
        public void TestSequence(int code, IEnumerable<SwitchButton> expect)
        {
            var result = TradeUtil.GetPresses(code);
            result.SequenceEqual(expect).Should().BeTrue();
        }
    }
}
