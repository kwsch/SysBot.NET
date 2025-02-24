using System.Collections.Generic;

namespace SysBot.Pokemon;

public sealed record PokeTradeSummary(string Summary, IList<PokeTradeSummaryDetail> Details, object? ExtraInfo = null)
{
    public PokeTradeSummary(string summary, object? extraInfo = null)
        : this(summary, [], extraInfo)
    {
    }
}
