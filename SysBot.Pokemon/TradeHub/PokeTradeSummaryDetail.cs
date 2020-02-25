namespace SysBot.Pokemon
{
    public sealed class PokeTradeSummaryDetail
    {
        public readonly string Heading;
        public readonly string Detail;

        public PokeTradeSummaryDetail(string heading, string detail)
        {
            Heading = heading;
            Detail = detail;
        }
    }
}