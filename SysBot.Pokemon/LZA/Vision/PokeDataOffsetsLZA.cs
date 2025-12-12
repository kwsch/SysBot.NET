using System.Collections.Generic;

namespace SysBot.Pokemon;

/// <summary>
/// Pokémon Legends: Z-A RAM offsets
/// </summary>
public class PokeDataOffsetsLZA
{
    public const string LZAGameVersion = "2.0.0";
    public const string LegendsZAID = "0100F43008C44000";

    public IReadOnlyList<long> BoxStartPokemonPointer           { get; } = [0x6105710, 0xB0, 0x978, 0x0];
    public IReadOnlyList<long> TextSpeedPointer                 { get; } = [0x6105710, 0xD8, 0x40];
    public IReadOnlyList<long> MyStatusPointer                  { get; } = [0x6105710, 0xA0, 0x40];
    public IReadOnlyList<long> PartyPointer                     { get; } = [0x6105710, 0x18, 0x1B0, 0xF0, 0x50, 0x30, 0x0];
    public IReadOnlyList<long> CurrentBoxPointer                { get; } = [0x6105710, 0xA8, 0x596];

    public IReadOnlyList<long> LinkTradePartnerPokemonPointer   { get; } = [0x6108630, 0x128, 0x30, 0x0];
    public IReadOnlyList<long> TradePartnerStatusPointer        { get; } = [0x6108630, 0x134];
    public IReadOnlyList<long> TradePartnerBackupNIDPointer     { get; } = [0x6108630, 0x108];

    public IReadOnlyList<long> LinkTradeCodeLengthPointer       { get; } = [0x6131138, 0x52];
    public IReadOnlyList<long> LinkTradeCodePointer             { get; } = [0x6131138, 0x30, 0x0];

    public IReadOnlyList<long> LinkTradePartnerDataPointer      { get; } = [0x40F73D8, 0x1D8, 0x30, 0xA0, 0x0];

    public const uint TradePartnerNIDShift           = 0x30;
    public const uint TradePartnerTIDShift           = 0x74;
    public const uint FallBackTradePartnerDataShift  = 0x598; // The data can often be found here if the main pointer fails.


    // Main offsets
    public const uint OverworldOffset = 0x6107858;
    public const uint MenuOffset      = 0x61289C0;
    public const uint ConnectedOffset = 0x612E358;

    public const int BoxFormatSlotSize = 0x148;
}
