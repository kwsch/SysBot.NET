using FluentAssertions;
using PKHeX.Core;
using SysBot.Pokemon.Helpers;
using Xunit;

namespace SysBot.Tests
{
    public class FileTradeHelperTests
    {

        [Fact]
        public void TestValidFileSize()
        {
            FileTradeHelper<PK9>.ValidFileSize(344).Should().Be(true);
            FileTradeHelper<PK9>.ValidFileSize(0).Should().Be(false);
            FileTradeHelper<PK9>.ValidFileSize(345).Should().Be(false);
            FileTradeHelper<PK9>.ValidFileSize(344 * 10).Should().Be(true);
            FileTradeHelper<PK9>.ValidFileSize(344 * 960).Should().Be(true);
            FileTradeHelper<PK9>.ValidFileSize(344 * 961).Should().Be(false);

            FileTradeHelper<PA8>.ValidFileSize(376).Should().Be(true);
            FileTradeHelper<PA8>.ValidFileSize(0).Should().Be(false);
            FileTradeHelper<PA8>.ValidFileSize(375).Should().Be(false);
            FileTradeHelper<PA8>.ValidFileSize(360 * 10).Should().Be(true);
            FileTradeHelper<PA8>.ValidFileSize(360 * 960).Should().Be(true);
            FileTradeHelper<PA8>.ValidFileSize(360 * 961).Should().Be(false);

            FileTradeHelper<PB8>.ValidFileSize(344).Should().Be(true);
            FileTradeHelper<PB8>.ValidFileSize(0).Should().Be(false);
            FileTradeHelper<PB8>.ValidFileSize(345).Should().Be(false);
            FileTradeHelper<PB8>.ValidFileSize(344 * 10).Should().Be(true);
            FileTradeHelper<PB8>.ValidFileSize(344 * 1200).Should().Be(true);
            FileTradeHelper<PB8>.ValidFileSize(344 * 1201).Should().Be(false);

            FileTradeHelper<PK8>.ValidFileSize(344).Should().Be(true);
            FileTradeHelper<PK8>.ValidFileSize(0).Should().Be(false);
            FileTradeHelper<PK8>.ValidFileSize(345).Should().Be(false);
            FileTradeHelper<PK8>.ValidFileSize(344 * 10).Should().Be(true);
            FileTradeHelper<PK8>.ValidFileSize(344 * 960).Should().Be(true);
            FileTradeHelper<PK8>.ValidFileSize(344 * 961).Should().Be(false);
        }

    }

}
