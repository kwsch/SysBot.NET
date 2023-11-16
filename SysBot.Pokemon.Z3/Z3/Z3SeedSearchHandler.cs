using PKHeX.Core;
using System;

namespace SysBot.Pokemon.Z3;

public class Z3SeedSearchHandler<T> : ISeedSearchHandler<T> where T : PKM, new()
{
    public void CalculateAndNotify(T pkm, PokeTradeDetail<T> detail, SeedCheckSettings settings, PokeRoutineExecutor<T> bot)
    {
        var ec = pkm.EncryptionConstant;
        var pid = pkm.PID;

        // Reorder the speed to be last.
        Span<int> IVs = stackalloc int[6];
        pkm.GetIVs(IVs);
        (IVs[5], IVs[3], IVs[4]) = (IVs[3], IVs[4], IVs[5]);

        if (settings.ShowAllZ3Results)
        {
            var matches = Z3Search.GetAllSeeds(ec, pid, IVs, settings.ResultDisplayMode);
            foreach (var match in matches)
            {
                var lump = new PokeTradeSummary("Calculated Seed:", match);
                detail.SendNotification(bot, lump);
            }
        }
        else
        {
            var match = Z3Search.GetFirstSeed(ec, pid, IVs, settings.ResultDisplayMode);
            var lump = new PokeTradeSummary("Calculated Seed:", match);
            detail.SendNotification(bot, lump);
        }
    }
}
