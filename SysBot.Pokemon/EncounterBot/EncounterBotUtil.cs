using System.Collections.Generic;

namespace SysBot.Pokemon
{
    public static class EncounterBotUtil
    {
        public static readonly IReadOnlyList<int> DisplayedAbilities = new[]
        {
            013, // CloudNine
            022, // Intimidate
            046, // Pressure
            104, // Mold Breaker
            108, // Forewarn
            127, // Unnerve
            250, // Mimicry
            256, // Neutralizing Gas
        };
    }
}