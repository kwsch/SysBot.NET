using System;

namespace SysBot.Pokemon
{
    public static class PokeDataOffsets
    {
        public const uint BoxStartOffset = 0x4293D8B0;
        public const uint CurrentBoxOffset = 0x4298e83E;
        public const uint TrainerDataOffset = 0x42935E48;
        public const uint SoftBanUnixTimespanOffset = 0x4298f9f8;
        public const uint IsConnectedOffset = 0x2f865c78;

        // Pokemon Encounter Offsets
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

        public const uint CurrentScreenOffset = 0x68dcbc90;

        public const uint CurrentScreen_Overworld = 0xFFFF5127;

        public const uint CurrentScreen_Box = 0xFF00D59B;
        public const uint CurrentScreen_Box_WaitingForOffer = 0xC800B483;
        public const uint CurrentScreen_Box_ConfirmOffer = 0xFF00B483;

        public const uint CurrentScreen_Softban = 0xFF000000;

        //public const uint CurrentScreen_YMenu = 0xFFFF7983;
        public const uint CurrentScreen_WildArea = 0xFFFFFFFF;
        public const uint CurrentScreen_RaidParty = 0xFF1461DB;

        /* Offset values for getting into a battle with Zacian/Zamazenta and fleeing to Tower.
         * There are 5 different colors of the sky, and we use the most common post-flee value. */
        public const uint CurrentScreen_Dog_0_3_FleeBattle = 0xFF390023;
        public const uint CurrentScreen_Dog_4_5_20_23_FleeBattle = 0xFF431424;
        public const uint CurrentScreen_Dog_6_8_FleeBattle = 0xFF20001C;
        public const uint CurrentScreen_Dog_9_18_FleeBattle = 0xFF642E32;
        public const uint CurrentScreen_Dog_19_FleeBattle = 0xFF3B002E;
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
