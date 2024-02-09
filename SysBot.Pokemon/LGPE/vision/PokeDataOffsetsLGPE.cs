using System.Collections.Generic;

namespace SysBot.Pokemon
{
    /// <summary>
    /// Pokémon Legends: Arceus RAM offsets
    /// </summary>
    public static class PokeDataOffsetsLGPE
    {
        public const string LetsGoPikachuID = "010003F003A34000";
        public const string LetsGoEeveeID = "0100187003A36000";

        public const uint TrainerDataOffset = 0x53321CF0;
        public const uint TextSpeedOffset = 0x53321EDC;

        public const int BoxFormatSlotSize = 0x104;
        public const int TrainerDataLength = 0x168;

        #region ScreenDetection
        public const uint LGPEBattleOverworldOffset = 0x5E07C8A8;    // Only used for checking if we've made it out of a battle.
        public const uint LGPEStandardOverworldOffset = 0x5E1CE550;  // Can be used for overworld checks in anything but battle.
        public const uint FleeMenuOffset = 0x60F2F119;

        public const uint waitingscreen = 0x15363d8;
        public const uint tradebuttons = 0x163bac0;
        public const uint ScreenOff = 0x1610E68;
        public const uint savescreen = 0x7250;
        public const uint savescreen2 = 0x6250;
        public static uint menuscreen = 0xD080;
        public static uint MysteryGiftscreen = 0xE080;
        public static uint SelectFarawayscreen = 0xA080;
        public static uint Boxscreen = 0xF080;
        public static uint waitingtotradescreen = 0x0080;
        public static uint waitingtotradescreen2 = 0x1080;
        public static uint scrollscreen = 0xB080;
        #endregion

        #region LGPE
        public const uint LGPEGoParkOffset = 0xAD5DC910;
        public const uint LGPEWildOffset = 0xAD5DC108;
        public const uint LGPEStaticOffset = 0x9A118D68;

        public const uint LGPELastSpawnSpeciesOffset = 0x419BB180;
        public const uint LGPELastSpawnFormOffset = 0x419BB182;
        public const uint LGPELastSpawnFlags = 0x419BB184;

        public const uint LGPECatchComboPokemon = 0x5E1CF4F8;
        public const uint LGPECatchComboCounter = 0x5E1CF500;

        public const uint LGPELureType = 0x53405D28;
        public const uint LGPELureCounter = 0x53405D2A;

        public const uint FortuneTellerNature = 0x53404C10;
        public const uint FortuneTellerEnabled = 0x53405CF8;
        
        public const uint TradePartnerData = 0x41A28240;
        public const uint TradePartnerData2 = 0x41A28078;
        public const uint OfferedPokemon = 0x41A22858;
        public const uint BoxSlot1 = 0x533675B0;

       
        #endregion

        #region RAM
        public static IReadOnlyList<long> LGPEBirdRNGPointer { get; } = new long[] { 0x160E410, 0x50, 0x90, 0x80, 0x110, 0x90 };

        public static IReadOnlyList<long> LGPECoordinatesPointer { get; } = new long[] { 0x16174C8, 0, 0x4, 0x58, 0xF0, 0x398, 0x60, 0x60 };
        #endregion
    }
}
