using System.Collections.Generic;
using System;

namespace SysBot.Pokemon
{
    /// <summary>
    /// Pokémon Legends: Arceus RAM offsets
    /// </summary>
    public class PokeDataOffsetsLA
    {
        public const string LegendsArceusID = "01001F5010DFA000";
        public IReadOnlyList<long> BoxStartPokemonPointer { get; } = new long[] { 0x427B470, 0x1F0, 0x68 };
        public IReadOnlyList<long> LinkTradePartnerPokemonPointer { get; } = new long[] { 0x427F888, 0x188, 0x78, 0x98, 0x58, 0x0 };
        public IReadOnlyList<long> LinkTradePartnerNamePointer { get; } = new long[] { 0x42ADCE0, 0xC8, 0x88 };
        public IReadOnlyList<long> LinkTradePartnerTIDPointer { get; } = new long[] { 0x42ADCE0, 0xC8, 0x78 };
        public IReadOnlyList<long> LinkTradePartnerNIDPointer { get; } = new long[] { 0x42AB168, 0xE0, 0x8 };
        public IReadOnlyList<long> MyStatusPointer { get; } = new long[] { 0x427B470, 0x218, 0x68 };
        public IReadOnlyList<long> TextSpeedPointer { get; } = new long[] { 0x427B470, 0x1E0, 0x68 };
        public IReadOnlyList<long> TradePartnerStatusPointer { get; } = new long[] { 0x427F888, 0x188, 0x78, 0xBC };
        public IReadOnlyList<long> SoftbanPointer { get; } = new long[] { 0x427B470, 0x268, 0x70 };
        public IReadOnlyList<long> OverworldPointer { get; } = new long[] { 0x4283E78, 0x1A9 };

        public const int BoxFormatSlotSize = 0x168;
    }
}
