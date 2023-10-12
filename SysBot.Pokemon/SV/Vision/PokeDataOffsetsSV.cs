using System.Collections.Generic;

namespace SysBot.Pokemon
{
    /// <summary>
    /// Pokémon Scarlet/Violet RAM offsets
    /// </summary>
    public class PokeDataOffsetsSV
    {
        public const string SVGameVersion = "2.0.2";
        public const string ScarletID = "0100A3D008C5C000";
        public const string VioletID  = "01008F6008C5E000";
        public IReadOnlyList<long> BoxStartPokemonPointer         { get; } = new long[] { 0x4617648, 0xD8, 0x8, 0xB8, 0x30, 0x9D0, 0x0};
        public IReadOnlyList<long> MyStatusPointer                { get; } = new long[] { 0x4617648, 0xD8, 0x8, 0xB8,  0x0, 0x40 };
        public IReadOnlyList<long> ConfigPointer                  { get; } = new long[] { 0x4617648, 0xD8, 0x8, 0xB8, 0xD0, 0x40 };
        public IReadOnlyList<long> CurrentBoxPointer              { get; } = new long[] { 0x4617648, 0xD8, 0x8, 0xB8, 0x28, 0x570 };
        public IReadOnlyList<long> LinkTradePartnerNIDPointer     { get; } = new long[] { 0x46404B8, 0xF8, 0x8 };
        public IReadOnlyList<long> LinkTradePartnerPokemonPointer { get; } = new long[] { 0x461BE58, 0x48, 0x58, 0x40, 0x148 };
        public IReadOnlyList<long> Trader1MyStatusPointer         { get; } = new long[] { 0x461BE58, 0x48, 0xB0, 0x0 };
        public IReadOnlyList<long> Trader2MyStatusPointer         { get; } = new long[] { 0x461BE58, 0x48, 0xE0, 0x0 };
        public IReadOnlyList<long> PortalBoxStatusPointer         { get; } = new long[] { 0x463BB60, 0x188, 0x350, 0xF0, 0x140, 0x78 };
        public IReadOnlyList<long> IsConnectedPointer             { get; } = new long[] { 0x461B3D8, 0x30 };
        public IReadOnlyList<long> OverworldPointer               { get; } = new long[] { 0x461CB18, 0x160, 0xE8, 0x28 };

        public const int BoxFormatSlotSize = 0x158;
        public const ulong LibAppletWeID = 0x010000000000100a; // One of the process IDs for the news.
    }
}
