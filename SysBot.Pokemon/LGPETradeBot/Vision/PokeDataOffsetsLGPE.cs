namespace SysBot.Pokemon;

/// <summary>
/// Pokémon Let's Go Pikachu!/Eevee!
/// </summary>
public class PokeDataOffsetsLGPE
{

//LET'S GO
        public const int EncryptedSize = 0x104;
        public const int TrainerSize = 0x168;
        public const uint FreezedValue = 0x1610EE0; //1 byte
        public const uint IsInOverworld = 0x163F694; //main
        public const uint IsInBattleScenario = 0x1EE067C; //main
        public const uint IsInTitleScreen = 0x160D4E0; //main
        public const uint IsInTrade = 0x1614F28; //main
        public const uint IsGiftFound = 0x1615928; //main
        public const uint StationaryBattleData = 0x9A118D68; //heap
        public const uint PokeData = 0x163EDC0; //main
        public const uint LastSpawn = 0x5E12B148; //heap
        public const uint EShinyValue = 0x7398C4; //main
        public const uint PShinyValue = 0x739864; //main
        public const uint PGeneratingFunction1 = 0x7398D0; //main
        public const uint PGeneratingFunction2 = 0x7398D4; //main
        public const uint PGeneratingFunction3 = 0x7398D8; //main
        public const uint PGeneratingFunction4 = 0x7398DC; //main
        public const uint PGeneratingFunction5 = 0x7398E0; //main
        public const uint PGeneratingFunction6 = 0x7398E4; //main
        public const uint PGeneratingFunction7 = 0x7398E8; //main
        public const uint EGeneratingFunction1 = 0x739930; //main
        public const uint EGeneratingFunction2 = 0x739934; //main
        public const uint EGeneratingFunction3 = 0x739938; //main
        public const uint EGeneratingFunction4 = 0x73993C; //main
        public const uint EGeneratingFunction5 = 0x739940; //main
        public const uint EGeneratingFunction6 = 0x739944; //main
        public const uint EGeneratingFunction7 = 0x739948; //main
        public const uint TrainerData = 0x53582030; //heap
        public const uint TradePartnerData = 0x41A28240;//heap
        public const uint TradePartnerData2 = 0x41A28078;//heap
        public const uint OfferedPokemon = 0x41A22858;//heap
        public const uint BoxSlot1 = 0x533675B0; //heap
        public const uint Money = 0x53324108; //heap
        public const uint NatureTellerEnabled = 0x53405CF8; //heap, 0 random nature, 4 set nature
        public const uint WildNature = 0x53404C10; //heap
        public const uint LGGameVersion = 0x53321DA8; //heap 0x1 = pika, 0x2 = eevee - Thanks Lincoln-LM!
        public const uint CatchingSpecies = 0x9A264598; //heap - Thanks Lincoln-LM!
        public const uint CatchCombo = 0x5E1CF500; //heap - Thanks Lincoln-LM!
        public const uint SpeciesCombo = 0x5E1CF4F8; //heap - Thanks Lincoln-LM!
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
        public static uint overworld = 0x78;
        public static uint scrollscreen = 0xB080;
        //Lets Go Pointers:
        public const string SpeciesComboPointer = "[[[[main+160E410]+50]+770]+40]+298";
        public const string CatchComboPointer = "[[[[main+160E410]+50]+840]+20]+1D0";
        public const int BoxFormatSlotSize = 0x158;

}
