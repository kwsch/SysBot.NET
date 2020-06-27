using System;
using SysBot.Base;

namespace SysBot.Pokemon
{
    public static class PokeDataOffsets
    {
        public const uint BoxStartOffset = 0x4506d890;
        public const uint CurrentBoxOffset = 0x450BE81E;
        public const uint TrainerDataOffset = 0x45061108;
        public const uint SoftBanUnixTimespanOffset = 0x44f46578;
        public const uint IsConnectedOffset = 0x2F71D593;
        public const uint TextSpeedOffset = 0x45061290;
        public const uint ItemTreasureAddress = 0x45060B60;

        // Raid Offsets
        // The dex number of the Pokémon the host currently has chosen. 
        // Details for each player span 0x30, so add 0x30 to get to the next offset.
        public const uint RaidP0PokemonOffset = 0x8398A174;
        // Add to each Pokémon offset.  AltForm used.
        public const uint RaidAltFormInc = 0x4;
        // Add to each Pokémon offset.  0 = male, 1 = female, 2 = genderless.
        public const uint RaidGenderIncr = 0x8;
        // Add to each Pokémon offset.  Bool for whether the Pokémon is shiny.
        public const uint RaidShinyIncr = 0xC;
        // Add to each Pokémon offset.  Bool for whether they have locked in their Pokémon.
        public const uint RaidLockedInIncr = 0x1C;
        public const uint RaidBossOffset = 0x8398A13C;

        // 1 when in a battle or raid, 0 otherwise.
        public const uint InBattleRaidOffset = 0x3F12850F;

        // Pokémon Encounter Offsets
        public const uint WildPokemonOffset = 0x8FEA3358;
        public const uint RaidPokemonOffset = 0x886A92C8;
        public const uint LegendaryPokemonOffset = 0x886BC058;

        // Link Trade Offsets
        public const uint LinkTradePartnerPokemonOffset = 0xAF285F68;
        public const uint LinkTradePartnerNameOffset = 0xAF28373C;
        public const uint LinkTradeSearchingOffset = 0x2f76c2b8;

        // Suprise Trade Offsets
        public const uint SurpriseTradePartnerPokemonOffset = 0x4505f790;

        public const uint SurpriseTradeLockSlot = 0x4505f8ec;
        public const uint SurpriseTradeLockBox = 0x4505f8e8;

        public const uint SurpriseTradeSearchOffset = 0x4505f8f4;
        public const uint SurpriseTradeSearch_Empty = 0x00000000;
        public const uint SurpriseTradeSearch_Searching = 0x01000000;
        public const uint SurpriseTradeSearch_Found = 0x0200012C;
        public const uint SurpriseTradePartnerNameOffset = 0x4505f8f8;

        /* Route 5 Daycare */
        public const uint DayCare_Wildarea_Step_Counter = 0x45117C64;
        public const uint DayCare_Wildarea_Egg_Is_Ready = 0x45117C70;

        /* Wild Area Daycare */
        public const uint DayCare_Route5_Step_Counter = 0x451179AC;
        public const uint DayCare_Route5_Egg_Is_Ready = 0x451179B8;

        public const int BoxFormatSlotSize = 0x158;
        public const int TrainerDataLength = 0x110;

        #region ScreenDetection
        // CurrentScreenOffset can be unreliable for Overworld; this one is 1 on Overworld and 0 otherwise.
        // Varies based on console language which is configured in Hub.
        // Default setting works for English, Dutch, Portuguese, and Russian
        public const uint OverworldOffset = 0x2F770528;
        public const uint OverworldOffsetFrench = 0x2F770718;
        public const uint OverworldOffsetGerman = 0x2F7707F8;
        public const uint OverworldOffsetSpanish = 0x2F7706E8;
        public const uint OverworldOffsetItalian = 0x2F7704A8;
        public const uint OverworldOffsetJapanese = 0x2F770688;
        public const uint OverworldOffsetChineseT = 0x2F76F6C8;
        public const uint OverworldOffsetChineseS = 0x2F76F728;
        public const uint OverworldOffsetKorean = 0x2F76FB28;

        // For detecting when we're able to interact with the menu in a battle.
        public const uint BattleMenuOffset = 0x69B99418;

        // Most screen detection checks the values at this offset.
        public const uint CurrentScreenOffset = 0x6b30f9e0;

        // Value goes between either of these; not game or area specific.
        public const uint CurrentScreen_Overworld1 = 0xFFFF5127;
        public const uint CurrentScreen_Overworld2 = 0xFFFFFFFF;

        public const uint CurrentScreen_Box1 = 0xFF00D59B;
        public const uint CurrentScreen_Box2 = 0xFF000000;
        public const uint CurrentScreen_Box_WaitingForOffer = 0xC800B483;
        public const uint CurrentScreen_Box_ConfirmOffer = 0xFF00B483;

        public const uint CurrentScreen_Softban = 0xFF000000;

        //public const uint CurrentScreen_YMenu = 0xFFFF7983;
        public const uint CurrentScreen_RaidParty = 0xFF1461DB;
        #endregion

        public static uint GetTrainerNameOffset(TradeMethod tradeMethod)
        {
            return tradeMethod switch
            {
                TradeMethod.LinkTrade => LinkTradePartnerNameOffset,
                TradeMethod.SupriseTrade => SurpriseTradePartnerNameOffset,
                _ => throw new ArgumentException(nameof(tradeMethod)),
            };
        }

        public static uint GetDaycareStepCounterOffset(SwordShieldDaycare daycare)
        {
            return daycare switch
            {
                SwordShieldDaycare.WildArea => DayCare_Wildarea_Step_Counter,
                SwordShieldDaycare.Route5 => DayCare_Route5_Step_Counter,
                _ => throw new ArgumentException(nameof(daycare)),
            };
        }

        public static uint GetDaycareEggIsReadyOffset(SwordShieldDaycare daycare)
        {
            return daycare switch
            {
                SwordShieldDaycare.WildArea => DayCare_Wildarea_Egg_Is_Ready,
                SwordShieldDaycare.Route5 => DayCare_Route5_Egg_Is_Ready,
                _ => throw new ArgumentException(nameof(daycare)),
            };
        }
    }
}
