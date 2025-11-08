using PKHeX.Core;
using System;

namespace SysBot.Pokemon.Z3;

public class Z3SeedSearchHandler<T> : ISeedSearchHandler<T> where T : PKM, new()
{
    public void CalculateAndNotify(T pkm, PokeTradeDetail<T> detail, SeedCheckSettings settings, PokeRoutineExecutor<T> bot)
    {
        // Let PKHeX try and deduce it first. Usually will be the best match.
        if (TryPKHeX(pkm, detail, settings, bot) && !settings.ShowAllZ3Results)
            return;

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

    private static bool TryPKHeX(T pk, PokeTradeDetail<T> detail, SeedCheckSettings settings, PokeRoutineExecutor<T> bot)
    {
        var la = new LegalityAnalysis(pk);
        var enc = la.Info.EncounterMatch;
        if (enc is not ISeedCorrelation64<PKM> x)
            return false;
        if (x.TryGetSeed(pk, out var seed) != SeedCorrelationResult.Success)
            return false;

        var flawless = enc is IFlawlessIVCount f ? f.FlawlessIVCount : 0;
        var result = new SeedSearchResult(Z3SearchResult.Success, seed, flawless, settings.ResultDisplayMode);
        var lump = new PokeTradeSummary("Calculated Seed:", result);
        detail.SendNotification(bot, lump);
        return true;
    }
}
