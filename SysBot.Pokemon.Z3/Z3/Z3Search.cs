using PKHeX.Core;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SysBot.Pokemon.Z3;

public static class Z3Search
{
    public static SeedSearchResult GetFirstSeed(uint ec, uint pid, Span<int> ivs, SeedCheckResults mode)
    {
        var seeds = GetSeeds(ec, pid);
        bool hasClosest = false;
        ulong closest = 0;
        foreach (var seed in seeds)
        {
            // Verify the IVs; at most 5 can match
            for (int i = 1; i <= 5; i++) // fixed IV count
            {
                if (IsMatch(seed, ivs, i))
                    return new SeedSearchResult(Z3SearchResult.Success, seed, i, mode);
            }
            hasClosest = true;
            closest = seed;
        }

        if (hasClosest)
            return new SeedSearchResult(Z3SearchResult.SeedMismatch, closest, 0, mode);
        return SeedSearchResult.None;
    }

    public static IList<SeedSearchResult> GetAllSeeds(uint ec, uint pid, Span<int> ivs, SeedCheckResults mode)
    {
        var result = new List<SeedSearchResult>();
        var seeds = GetSeeds(ec, pid);
        foreach (var seed in seeds)
        {
            // Verify the IVs; at most 5 can match
            bool added = false;
            for (int i = 1; i <= 5; i++) // fixed IV count
            {
                if (!IsMatch(seed, ivs, i))
                    continue;
                result.Add(new SeedSearchResult(Z3SearchResult.Success, seed, i, mode));
                added = true;
            }
            if (!added)
                result.Add(new SeedSearchResult(Z3SearchResult.SeedMismatch, seed, 0, mode));
        }

        if (result.Count == 0)
            result.Add(SeedSearchResult.None);
        else if (result.Any(z => z.Type == Z3SearchResult.Success))
            result.RemoveAll(z => z.Type != Z3SearchResult.Success);
        return result;
    }

    public static IEnumerable<ulong> GetSeeds(uint ec, uint pid)
    {
        foreach (var seed in FindPotentialSeeds(ec, pid))
            yield return seed;
        foreach (var seed in FindPotentialSeeds(ec, pid ^ 0x10000000))
            yield return seed;
    }

    public static IEnumerable<ulong> FindPotentialSeeds(uint ec, uint pid)
    {
        var seeds = new XoroMachineSkip(ec, pid);
        foreach (var seed in seeds)
            yield return seed;
    }

    public static bool IsMatch(ulong seed, Span<int> ivs, int fixed_ivs)
    {
        var rng = new Xoroshiro128Plus(seed);
        rng.NextInt(); // EC
        rng.NextInt(); // TID
        rng.NextInt(); // PID
        int[] check_ivs = [-1, -1, -1, -1, -1, -1];
        for (int i = 0; i < fixed_ivs; i++)
        {
            int slot;
            do slot = (int)rng.NextInt(6);
            while (check_ivs[slot] != -1);

            if (ivs[slot] != 31)
                return false;

            check_ivs[slot] = 31;
        }
        for (int i = 0; i < 6; i++)
        {
            if (check_ivs[i] != -1)
                continue; // already verified?

            uint iv = (uint)rng.NextInt(32);
            if (iv != ivs[i])
                return false;
        }
        return true;
    }
}
