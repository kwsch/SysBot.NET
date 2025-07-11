using System;
using System.Collections.Generic;

namespace SysBot.Pokemon;

public sealed record PokeTradeSummary
{
    public readonly string Summary;
    public readonly IList<PokeTradeSummaryDetail> Details;
    public readonly object? ExtraInfo;

    public PokeTradeSummary(string summary, IList<PokeTradeSummaryDetail> details, object? extraInfo = null)
    {
        Summary = summary;
        Details = details;
        ExtraInfo = extraInfo;
    }

    public PokeTradeSummary(string summary, object? extraInfo = null)
    {
        Summary = summary;
        Details = Array.Empty<PokeTradeSummaryDetail>();
        ExtraInfo = extraInfo;
    }
}
