using System.Collections.Generic;

namespace SysBot.Pokemon;

/// <summary>
/// Pokémon Scarlet/Violet RAM offsets
/// </summary>
public class PokeDataOffsetsSV
{
    public const string SVGameVersion = "3.0.1";
    public const string ScarletID = "0100A3D008C5C000";
    public const string VioletID  = "01008F6008C5E000";
    public IReadOnlyList<long> BoxStartPokemonPointer         { get; } = [0x47350d8, 0xD8, 0x8, 0xB8, 0x30, 0x9D0, 0x0];
    public IReadOnlyList<long> MyStatusPointer                { get; } = [0x47350d8, 0xD8, 0x8, 0xB8,  0x0, 0x40];
    public IReadOnlyList<long> ConfigPointer                  { get; } = [0x47350d8, 0xD8, 0x8, 0xB8, 0xD0, 0x40];
    public IReadOnlyList<long> CurrentBoxPointer              { get; } = [0x47350d8, 0xD8, 0x8, 0xB8, 0x28, 0x570];
    public IReadOnlyList<long> LinkTradePartnerNIDPointer     { get; } = [0x475EA28, 0xF8, 0x8];
    public IReadOnlyList<long> LinkTradePartnerPokemonPointer { get; } = [0x473A110, 0x48, 0x58, 0x40, 0x148];
    public IReadOnlyList<long> Trader1MyStatusPointer         { get; } = [0x473A110, 0x48, 0xB0, 0x0];
    public IReadOnlyList<long> Trader2MyStatusPointer         { get; } = [0x473A110, 0x48, 0xE0, 0x0];
    public IReadOnlyList<long> PortalBoxStatusPointer         { get; } = [0x475A0D0, 0x188, 0x350, 0xF0, 0x140, 0x78];
    public IReadOnlyList<long> IsConnectedPointer             { get; } = [0x4739648, 0x30];
    public IReadOnlyList<long> OverworldPointer               { get; } = [0x473ADE0, 0x160, 0xE8, 0x28];

    public const int BoxFormatSlotSize = 0x158;
    public const ulong LibAppletWeID = 0x010000000000100a; // One of the process IDs for the news.
}
