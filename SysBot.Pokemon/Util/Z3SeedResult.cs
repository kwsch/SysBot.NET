using System;
using System.Collections.Generic;
using PKHeX.Core;

namespace SysBot.Pokemon
{
    public class Z3SeedResult
    {
        public static readonly Z3SeedResult None = new Z3SeedResult(Z3SearchResult.SeedNone, default, 0);

        public readonly Z3SearchResult Type;
        public readonly ulong Seed;
        public readonly int FlawlessIVCount;

        public Z3SeedResult(Z3SearchResult type, ulong seed, int ivCount)
        {
            Type = type;
            Seed = seed;
            FlawlessIVCount = ivCount;
        }

        public override string ToString()
        {
            return Type switch
            {
                Z3SearchResult.SeedMismatch => $"Seed found, but not an exact match {Seed:X16}",
                Z3SearchResult.Success => string.Join(Environment.NewLine, GetLines()),
                _ => "The Pokémon is not a raid Pokémon!"
            };
        }

        private IEnumerable<string> GetLines()
        {
            var first = $"Seed: {Seed:X16}";
            if (FlawlessIVCount >= 1)
                first += $", IVCount: {FlawlessIVCount}";
            yield return first;
            yield return $"Next Shiny Frame: {Z3Search.GetNextShinyFrame(Seed, out var type)}";
            var shinytype = type == 1 ? "Star" : "Square";
            yield return $"Shiny Type: {shinytype}";
        }

        public Shiny GetShinyType()
        {
            Z3Search.GetNextShinyFrame(Seed, out var type);
            return type == 1 ? Shiny.AlwaysStar : Shiny.AlwaysSquare;
        }
    }
}