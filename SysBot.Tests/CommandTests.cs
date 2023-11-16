using System;
using FluentAssertions;
using SysBot.Base;
using Xunit;

namespace SysBot.Tests;

public class CommandTests
{
    [Fact]
    public void ButtonTestA()
    {
        var poke = SwitchCommand.Click(SwitchButton.A);
        var expect = "click A\r\n"u8;
        expect.SequenceEqual(poke).Should().BeTrue();
    }

    [Fact]
    public void ButtonTestX()
    {
        var poke = SwitchCommand.Click(SwitchButton.X);
        var expect = "click X\r\n"u8;
        expect.SequenceEqual(poke).Should().BeTrue();
    }
}

public class DecodeTests
{
    [Fact]
    public void DecodeTest()
    {
        ReadOnlySpan<byte> raw = [0x30, 0x31, 0x30, 0x32, 0x30, 0x33, 0x30, 0x34, 0x30, 0x41, 0x30, 0x46];
        ReadOnlySpan<byte> expect = [1, 2, 3, 4, 10, 15];

        var convert = Decoder.ConvertHexByteStringToBytes(raw);
        expect.SequenceEqual(convert).Should().BeTrue();
    }
}
