using System.Collections.Generic;

namespace SysBot.Pokemon
{
    public abstract class BasePokeDataOffsetsBS : IPokeDataOffsetsBS
    {
        public const string BSGameVersion = "1.3.0";
        public const string ShiningPearlID = "010018E011D92000";
        public const string BrilliantDiamondID = "0100000011D90000";

        public abstract IReadOnlyList<long> BoxStartPokemonPointer { get; }
        public abstract IReadOnlyList<long> LinkTradePartnerPokemonPointer { get; }
        public abstract IReadOnlyList<long> LinkTradePartnerNamePointer { get; }
        public abstract IReadOnlyList<long> LinkTradePartnerIDPointer { get; }
        public abstract IReadOnlyList<long> LinkTradePartnerParamPointer { get; }
        public abstract IReadOnlyList<long> LinkTradePartnerNIDPointer { get; }
        public abstract IReadOnlyList<long> SceneIDPointer { get; }

        // Union Work - Detects states in the Union Room
        public abstract IReadOnlyList<long> UnionWorkIsGamingPointer { get; }
        public abstract IReadOnlyList<long> UnionWorkIsTalkingPointer { get; }
        public abstract IReadOnlyList<long> UnionWorkPenaltyPointer { get; }
        public abstract IReadOnlyList<long> MyStatusTrainerPointer { get; }
        public abstract IReadOnlyList<long> MyStatusTIDPointer { get; }
        public abstract IReadOnlyList<long> ConfigTextSpeedPointer { get; }
        public abstract IReadOnlyList<long> ConfigLanguagePointer { get; }

        // SceneID enums
        public const byte SceneID_Field = 0;
        public const byte SceneID_Room = 1;
        public const byte SceneID_Battle = 2;
        public const byte SceneID_Title = 3;
        public const byte SceneID_Opening = 4;
        public const byte SceneID_Contest = 5;
        public const byte SceneID_DigFossil = 6;
        public const byte SceneID_SealPreview = 7;
        public const byte SceneID_EvolveDemo = 8;
        public const byte SceneID_HatchDemo = 9;
        public const byte SceneID_GMS = 10;

        public const int BoxFormatSlotSize = 0x158;
    }
}
