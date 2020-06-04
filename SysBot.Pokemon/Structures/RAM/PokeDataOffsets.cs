using System;
using SysBot.Base;

namespace SysBot.Pokemon
{
    public static class PokeDataOffsets
    {
        public const uint BoxStartOffset = 0x4293D8B0;
        public const uint CurrentBoxOffset = 0x4298e83E;
        public const uint TrainerDataOffset = 0x42935E48;
        public const uint SoftBanUnixTimespanOffset = 0x4298f9f8;
        public const uint IsConnectedOffset = 0x2F864118;
        public const uint TextSpeedOffset = 0x42935FD0;

        // Raid Offsets
        // The dex number of the Pokémon the host currently has chosen. 
        // Details for each player span 0x30, so add 0x30 to get to the next offset.
        public const uint RaidP0PokemonOffset = 0x80F48154;
        // Add to each Pokémon offset.  AltForm used.
        public const uint RaidAltFormInc = 0x4;
        // Add to each Pokémon offset.  0 = male, 1 = female, 2 = genderless.
        public const uint RaidGenderIncr = 0x8;
        // Add to each Pokémon offset.  Bool for whether the Pokémon is shiny.
        public const uint RaidShinyIncr = 0xC;
        // Add to each Pokémon offset.  Bool for whether they have locked in their Pokémon.
        public const uint RaidLockedInIncr = 0x1C;
        public const uint RaidBossOffset = 0x80F4811C;

        // Vary by game. They are 1 when in a battle or raid, 0 otherwise.
        public const uint InBattleRaidOffsetSH = 0x3DD26104;
        public const uint InBattleRaidOffsetSW = 0x3DD26109;

        // Pokémon Encounter Offsets
        public const uint WildPokemonOffset = 0x8D45C648;
        public const uint RaidPokemonOffset = 0x85C7AB08;
        public const uint LegendaryPokemonOffset = 0x85C74F88;

        // Link Trade Offsets
        public const uint LinkTradePartnerPokemonOffset = 0x2E32206A;
        public const uint LinkTradePartnerNameOffset = 0xAC84173C;
        public const uint LinkTradeSearchingOffset = 0x41d8e17c;
        public const uint LinkTradeFoundOffset = 0x4293aca4; // byte[0] & 0xF = 8

        // Suprise Trade Offsets
        public const uint SurpriseTradePartnerPokemonOffset = 0x429344d0;

        public const uint SurpriseTradeLockSlot = 0x4293462c;
        public const uint SurpriseTradeLockBox = 0x42934628;

        public const uint SurpriseTradeSearchOffset = 0x42934634;
        public const uint SurpriseTradeSearch_Empty = 0x00000000;
        public const uint SurpriseTradeSearch_Searching = 0x01000000;
        public const uint SurpriseTradeSearch_Found = 0x0200012C;

        public const uint SurpriseTradePartnerNameOffset = 0x42934638;
        public const uint ItemTreasureAddress = 0x429358A0;

        /* Route 5 Daycare */
        //public const uint DayCareSlot_1_WildArea_Present = 0x429e4EA8;
        //public const uint DayCareSlot_2_WildArea_Present = 0x429e4ff1;

        //public const uint DayCareSlot_1_WildArea = 0x429E4EA9;
        //public const uint DayCareSlot_2_WildArea = 0x429e4ff2;

        //public const uint DayCare_WildArea_Unknown = 0x429e513a;

        public const uint DayCare_Wildarea_Step_Counter = 0x429e513c;

        //public const uint DayCare_WildArea_EggSeed = 0x429e5140;
        public const uint DayCare_Wildarea_Egg_Is_Ready = 0x429e5148;

        /* Wild Area Daycare */
        //public const uint DayCareSlot_1_Route5_Present = 0x429e4bf0;
        //public const uint DayCareSlot_2_Route5_Present = 0x429e4d39;

        //public const uint DayCareSlot_1_Route5 = 0x429e4bf1;
        //public const uint DayCareSlot_2_Route5 = 0x429e4d3a;

        //public const uint DayCare_Route5_Unknown = 0x429e4e82;

        public const uint DayCare_Route5_Step_Counter = 0x429e4e84;

        //public const uint DayCare_Route5_EggSeed = 0x429e4e88;
        public const uint DayCare_Route5_Egg_Is_Ready = 0x429e4e90;

        public const int BoxFormatSlotSize = 0x158;
        public const int TrainerDataLength = 0x110;

        #region ScreenDetection
        /*
         * Not used for now, might be used someday
        public const uint MenuOffset = 0x00000036;
        public const uint MenuOpen = 0x7DC80000;
        public const uint MenuClosed = 0xBBE00000;
        */

        // CurrentScreenOffset can be unreliable for Overworld; this one is 1 on Overworld and 0 otherwise.
        // Varies based on console language which is configured in Hub.
        // Default setting works for English, Dutch, Portuguese, and Russian
        public const uint OverworldOffset = 0x2E36E474;
        public const uint OverworldOffsetFrench = 0x2E36E634;
        public const uint OverworldOffsetGerman = 0x2E36E734;
        public const uint OverworldOffsetSpanish = 0x2E36E634;
        public const uint OverworldOffsetItalian = 0x2E36E404;
        public const uint OverworldOffsetJapanese = 0x2E36E5F4;
        public const uint OverworldOffsetChineseT = 0x2E36D664;
        public const uint OverworldOffsetChineseS = 0x2E36D6C4;
        public const uint OverworldOffsetKorean = 0x2E36DAB4;

        // For detecting when we're able to interact with the menu in a battle.
        // Offsets flicker while battle is loading, best to wait close to when the menu appears.
        public const uint BattleMenuOffset = 0x691372E4;
        public const uint BattleMenuOffsetDog = 0x6759E550;

        // Most screen detection checks the values at this offset.
        public const uint CurrentScreenOffset = 0x68dcbc90;

        public const uint CurrentScreen_Overworld = 0xFFFF5127;

        public const uint CurrentScreen_Box = 0xFF00D59B;
        public const uint CurrentScreen_Box_WaitingForOffer = 0xC800B483;
        public const uint CurrentScreen_Box_ConfirmOffer = 0xFF00B483;

        public const uint CurrentScreen_Softban = 0xFF000000;

        //public const uint CurrentScreen_YMenu = 0xFFFF7983;
        public const uint CurrentScreen_WildArea = 0xFFFFFFFF;
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

        public static uint GetOverworldOffset(ConsoleLanguageParameter value)
        {
            return value switch
            {
                ConsoleLanguageParameter.French => OverworldOffsetFrench,
                ConsoleLanguageParameter.German => OverworldOffsetGerman,
                ConsoleLanguageParameter.Spanish => OverworldOffsetSpanish,
                ConsoleLanguageParameter.Italian => OverworldOffsetItalian,
                ConsoleLanguageParameter.Japanese => OverworldOffsetJapanese,
                ConsoleLanguageParameter.ChineseTraditional => OverworldOffsetChineseT,
                ConsoleLanguageParameter.ChineseSimplified => OverworldOffsetChineseS,
                ConsoleLanguageParameter.Korean => OverworldOffsetKorean,
                _ => OverworldOffset,
            };
        }
    }
}
