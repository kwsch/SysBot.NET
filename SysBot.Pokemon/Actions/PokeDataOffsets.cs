using System;

namespace SysBot.Pokemon
{
    public static class PokeDataOffsets
    {
        public const uint Box1Slot1 = 0x4293D8B0;
        public const uint TrainerDataOffset = 0x42935E48;
        public const uint ShownTradeDataOffset = 0x2E32206A;
        public const uint TradePartnerNameOffset = 0xAC84173C;

        public const uint IsConnected = 0x2f865c78;

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

        /* Y-COM Menu/ X Menu Detection */
        public const uint MenuOffset = 0x00000036;
        public const uint MenuOpen = 0x7DC80000;
        public const uint MenuClosed = 0xBBE00000;

        public const uint ScreenStateOffset = 0x1074b8;
        public static uint Overworld; // Also Loading in some cases  
        public static uint BoxView => Overworld + 1;
        public static uint DuringTrade => Overworld + 6;
        public static uint TradeEvo => Overworld + 2;
        #endregion

        public static uint GetDaycareOffset(SwordShieldDaycare daycare)
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
