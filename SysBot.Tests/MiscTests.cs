using FluentAssertions;
using SysBot.Base;
using SysBot.Pokemon;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using static SysBot.Base.SwitchButton;

namespace SysBot.Tests;

public class MiscTests
{
    [Theory]
    [InlineData(81113333, 0, 8)]
    [InlineData(12345678, 0, 1)]
    [InlineData(12345678, 1, 2)]
    [InlineData(12345678, 2, 3)]
    [InlineData(12345678, 3, 4)]
    [InlineData(12345678, 4, 5)]
    [InlineData(12345678, 5, 6)]
    [InlineData(12345678, 6, 7)]
    [InlineData(12345678, 7, 8)]
    public void TestDigit(int code, int digit, int expect)
    {
        var result = TradeUtil.GetCodeDigit(code, digit);
        result.Should().Be(expect);
    }

    [Theory]
    [InlineData(00001111, new[] { DDOWN, DDOWN, DDOWN, A, A, A, A, DUP, DUP, DUP, DLEFT, A, A, A, A })]
    [InlineData(00000000, new[] { DDOWN, DDOWN, DDOWN, A, A, A, A, A, A, A, A })]
    [InlineData(12345678, new[] { A, DRIGHT, A, DRIGHT, A, DDOWN, DLEFT, DLEFT, A, DRIGHT, A, DRIGHT, A, DDOWN, DLEFT, DLEFT, A, DRIGHT, A })]
    [InlineData(00000006, new[] { DDOWN, DDOWN, DDOWN, A, A, A, A, A, A, A, DUP, DUP, DRIGHT, A })]
    [InlineData(31500103, new[] { DRIGHT, DRIGHT, A, DLEFT, DLEFT, A, DDOWN, DRIGHT, A, DDOWN, DDOWN, A, A, DUP, DUP, DUP, DLEFT, A, DDOWN, DDOWN, DDOWN, A, DUP, DUP, DUP, DRIGHT, A })]
    [InlineData(91372193, new[] { DDOWN, DDOWN, DRIGHT, DRIGHT, A, DUP, DUP, DLEFT, DLEFT, A, DRIGHT, DRIGHT, A, DDOWN, DDOWN, DLEFT, DLEFT, A, DUP, DUP, DRIGHT, A, DLEFT, A, DDOWN, DDOWN, DRIGHT, DRIGHT, A, DUP, DUP, A })]
    public void TestSequence(int code, IEnumerable<SwitchButton> expect)
    {
        var result = TradeUtil.GetPresses(code);
        result.SequenceEqual(expect).Should().BeTrue();
    }
}
