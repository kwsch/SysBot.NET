namespace SysBot.Pokemon;

public sealed record PokeTradeSummaryDetail
{
    public readonly string Heading;
    public readonly string Detail;

    public PokeTradeSummaryDetail(string heading, string detail)
    {
        Heading = heading;
        Detail = detail;
    }
}
