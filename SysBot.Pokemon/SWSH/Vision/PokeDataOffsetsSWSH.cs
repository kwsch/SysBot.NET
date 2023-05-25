using System;
using System.Collections.Generic;

namespace SysBot.Pokemon
{
    /// <summary>
    /// Sword &amp; Shield RAM offsets
    /// </summary>
    public class PokeDataOffsetsSWSH
    {
        public const string SWSHGameVersion = "1.3.2";
        public const string SwordID = "0100ABF008968000";
        public const string ShieldID = "01008DB008C2C000";

        public const uint BoxStartOffset = 0x45075880;
        public const uint CurrentBoxOffset = 0x450C680E;
        public const uint TrainerDataOffset = 0x45068F18;
        public const uint SoftBanUnixTimespanOffset = 0x450C89E8;
        public const uint IsConnectedOffset = 0x30c7cca8;
        public const uint TextSpeedOffset = 0x450690A0;
        public const uint ItemTreasureAddress = 0x45068970;

        // Raid Offsets
        // The dex number of the Pokémon the host currently has chosen. 
        // Details for each player span 0x30, so add 0x30 to get to the next offset.
        public const uint RaidP0PokemonOffset = 0x8398A294;
        // Add to each Pokémon offset.  AltForm used.
        public const uint RaidAltFormInc = 0x4;
        // Add to each Pokémon offset.  0 = male, 1 = female, 2 = genderless.
        public const uint RaidGenderIncr = 0x8;
        // Add to each Pokémon offset.  Bool for whether the Pokémon is shiny.
        public const uint RaidShinyIncr = 0xC;
        // Add to each Pokémon offset.  Bool for whether they have locked in their Pokémon.
        public const uint RaidLockedInIncr = 0x1C;
        public const uint RaidBossOffset = 0x8398A25C;

        // 0 when not in a battle or raid, 0x40 or 0x41 otherwise.
        public const uint InBattleRaidOffsetSW = 0x3F128624;
        public const uint InBattleRaidOffsetSH = 0x3F128626;

        // Pokémon Encounter Offsets
        public const uint WildPokemonOffset = 0x8FEA3648;
        public const uint RaidPokemonOffset = 0x886A95B8;
        public const uint LegendaryPokemonOffset = 0x886BC348;

        // Link Trade Offsets
        public const uint LinkTradePartnerPokemonOffset = 0xAF286078;
        public const uint LinkTradePartnerNameOffset = 0xAF28384C;
        public const uint LinkTradePartnerTIDSIDOffset = LinkTradePartnerNameOffset - 0x8;
        public const uint LinkTradePartnerNIDOffset = 0xAF2846B0;
        public const uint LinkTradeSearchingOffset = 0x2F76C3C8;

        // Surprise Trade Offsets
        public const uint SurpriseTradePartnerPokemonOffset = 0x450675a0;
        public const uint SurpriseTradePartnerNameOffset = 0x45067708;
        public const uint SurpriseTradePartnerTIDSIDOffset = SurpriseTradePartnerNameOffset - 0x8;

        public const uint SurpriseTradeSearchOffset = 0x45067704;
        public const uint SurpriseTradeSearch_Empty = 0x00000000;
        public const uint SurpriseTradeSearch_Searching = 0x01000000;
        public const uint SurpriseTradeSearch_Found = 0x0200012C;

        public const uint SurpriseTradeLockSlot = 0x450676fc;
        public const uint SurpriseTradeLockBox = 0x450676f8;

        /* Route 5 Daycare */
        public const uint DayCare_Route5_Step_Counter = 0x4511F99C;
        public const uint DayCare_Route5_Egg_Is_Ready = 0x4511F9A8;

        public const int BoxFormatSlotSize = 0x158;
        public const int TrainerDataLength = 0x110;

        #region ScreenDetection
        // Stable overworld detection. Value is 1 on overworld and 0 otherwise.
        public IReadOnlyList<long> OverworldPointer { get; } = new long[] { 0x2636678, 0xC0, 0x80 };

        // For detecting when we're on the in-battle menu, so we can flee.
        public const uint BattleMenuOffset = 0x6B578EDC;

        // Original screen detection offset.
        public const uint CurrentScreenOffset = 0x6B30FA00;
        // Used for checking if we're in a box. It can be either value for different users.
        public const uint CurrentScreen_Box1 = 0xFF00D59B;
        public const uint CurrentScreen_Box2 = 0xFF000000;
        // Value when user is softbanned.
        public const uint CurrentScreen_Softban = 0xFF000000;
        #endregion

        public static uint GetTrainerNameOffset(TradeMethod tradeMethod) => tradeMethod switch
        {
            TradeMethod.LinkTrade => LinkTradePartnerNameOffset,
            TradeMethod.SurpriseTrade => SurpriseTradePartnerNameOffset,
            _ => throw new ArgumentException("Trainer name offset is not available for this trade method.", nameof(tradeMethod)),
        };

        public static uint GetTrainerTIDSIDOffset(TradeMethod tradeMethod) => tradeMethod switch
        {
            TradeMethod.LinkTrade => LinkTradePartnerTIDSIDOffset,
            TradeMethod.SurpriseTrade => SurpriseTradePartnerTIDSIDOffset,
            _ => throw new ArgumentException("Trainer TID/SID offset is not available for this trade method.", nameof(tradeMethod)),
        };
    }
}
