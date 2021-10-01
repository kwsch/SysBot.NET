using System;
using System.Collections.Generic;

namespace SysBot.Pokemon
{
    public class SeedSearchResult
    {
        public static readonly SeedSearchResult None = new(Z3SearchResult.SeedNone, default, 0, SeedCheckResults.ClosestOnly);

        public readonly Z3SearchResult Type;
        public readonly ulong Seed;
        public readonly int FlawlessIVCount;
        public readonly SeedCheckResults Mode;

        public SeedSearchResult(Z3SearchResult type, ulong seed, int ivCount, SeedCheckResults mode)
        {
            Type = type;
            Seed = seed;
            FlawlessIVCount = ivCount;
            Mode = mode;
        }

        public override string ToString()
        {
            return Type switch
            {
                Z3SearchResult.SeedMismatch => $"Seed found, but not an exact match {Seed:X16}",
                Z3SearchResult.Success => string.Join(Environment.NewLine, GetLines()),
                _ => "The Pokémon is not a raid Pokémon!",
            };
        }

        private IEnumerable<string> GetLines()
        {
            if (FlawlessIVCount >= 1)
                yield return $"IVCount: {FlawlessIVCount}";
            yield return "Spreads are listed by flawless IV count.";

            SeedSearchUtil.GetShinyFrames(Seed, out int[] frames, out uint[] type, out List<uint[,]> IVs, Mode);

            for (int i = 0; i < 3 && frames[i] != 0; i++)
            {
                var shinytype = type[i] == 1 ? "Star" : "Square";
                yield return $"\nFrame: {frames[i]} - {shinytype}";

                for (int ivcount = 0; ivcount < 5; ivcount++)
                {
                    var ivlist = $"{ivcount + 1} - ";
                    for (int j = 0; j < 6; j++)
                    {
                        ivlist += IVs[i][ivcount, j];
                        if (j < 5)
                            ivlist += "/";
                    }
                    yield return $"{ivlist}";
                }
            }
        }
    }
}