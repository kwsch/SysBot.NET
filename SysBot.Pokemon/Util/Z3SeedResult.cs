using System;
using System.Collections.Generic;

namespace SysBot.Pokemon
{
    public class Z3SeedResult
    {
        public static readonly Z3SeedResult None = new Z3SeedResult(Z3SearchResult.SeedNone, default, 0);

        public readonly Z3SearchResult Type;
        public readonly ulong Seed;
        public readonly int IVCount;

        public Z3SeedResult(Z3SearchResult type, ulong seed, int ivCount)
        {
            Type = type;
            Seed = seed;
            IVCount = ivCount;
        }

        public override string ToString()
        {
            return Type switch
            {
                Z3SearchResult.SeedMismatch => $"Seed found, but not an exact match {Seed:X16}",
                Z3SearchResult.Success => string.Join(Environment.NewLine, GetLines()),
                _ => "The Pokemon is not a raid Pokemon!"
            };
        }

        private IEnumerable<string> GetLines()
        {
            yield return $"Seed: {Seed:X16}";
            yield return $"Next Shiny Frame: {Z3Search.GetNextShinyFrame(Seed, out var type)}";
            var shinytype = type == 1 ? "Star" : "Square";
            yield return $"Shiny Type: {shinytype}";
        }
    }
}