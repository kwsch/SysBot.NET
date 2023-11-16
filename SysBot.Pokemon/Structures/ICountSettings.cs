using System.Collections.Generic;

namespace SysBot.Pokemon;

public interface ICountSettings
{
    bool EmitCountsOnStatusCheck { get; }
    IEnumerable<string> GetNonZeroCounts();
}

public interface ICountBot
{
    public ICountSettings Counts { get; }
}

public interface IEncounterBot : ICountBot
{
    public void Acknowledge();
}
