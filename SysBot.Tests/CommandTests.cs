using FluentAssertions;
using SysBot.Base;
using System.Linq;
using Xunit;

namespace SysBot.Tests
{
    public class CommandTests
    {
        [Fact]
        public void ButtonTestA()
        {
            var poke = SwitchCommand.Click(SwitchButton.A);
            var expect = new byte[] { 0x63, 0x6C, 0x69, 0x63, 0x6B, 0x20, 0x41, 0x0D, 0x0A }; // "click A\r\n"
            poke.SequenceEqual(expect).Should().BeTrue();
        }

        [Fact]
        public void ButtonTestX()
        {
            var poke = SwitchCommand.Click(SwitchButton.X);
            var expect = new byte[] { 0x63, 0x6C, 0x69, 0x63, 0x6B, 0x20, 0x58, 0x0D, 0x0A }; // "click X\r\n"
            poke.SequenceEqual(expect).Should().BeTrue();
        }
    }

    public class DecodeTests
    {
        [Fact]
        public void DecodeTest()
        {
            byte[] raw = { 0x30, 0x31, 0x30, 0x32, 0x30, 0x33, 0x30, 0x34, 0x30, 0x41, 0x30, 0x46 };
            byte[] expect = { 1, 2, 3, 4, 10, 15 };

            var convert = Decoder.ConvertHexByteStringToBytes(raw);
            convert.SequenceEqual(expect).Should().BeTrue();
        }
    }
}
