using System.Collections.Generic;

namespace SysBot.Pokemon
{
    public class PokeDataOffsetsBS_BD : BasePokeDataOffsetsBS
    {
        public override IReadOnlyList<long> BoxStartPokemonPointer { get; } = new long[] { 0x4C1DCF8, 0xB8, 0x10, 0xA0, 0x20, 0x20, 0x20 };

        public override IReadOnlyList<long> LinkTradePartnerPokemonPointer { get; } = new long[] { 0x4C19350, 0xB8, 0x8, 0x20 };
        public override IReadOnlyList<long> LinkTradePartnerNamePointer { get; } = new long[] { 0x4C1E7D8, 0xB8, 0x30, 0x108, 0x28, 0x90, 0x18, 0x0 };
        public override IReadOnlyList<long> LinkTradePartnerIDPointer { get; } = new long[] { 0x4C1E7D8, 0xB8, 0x30, 0x108, 0x28, 0x90, 0x10 };
        public override IReadOnlyList<long> LinkTradePartnerParamPointer { get; } = new long[] { 0x4C1E7D8, 0xB8, 0x30, 0x108, 0x28, 0x90 };
        public override IReadOnlyList<long> LinkTradePartnerNIDPointer { get; } = new long[] { 0x4FFE810, 0x70, 0x168, 0x40 };

        public override IReadOnlyList<long> SceneIDPointer { get; } = new long[] { 0x4C12B70, 0xB8, 0x18 };

        // Union Work - Detects states in the Union Room
        public override IReadOnlyList<long> UnionWorkIsGamingPointer { get; } = new long[] { 0x4C12CA8, 0xB8, 0x3C }; // 1 when loaded into Union Room, 0 otherwise
        public override IReadOnlyList<long> UnionWorkIsTalkingPointer { get; } = new long[] { 0x4C12CA8, 0xB8, 0x81 };  // 1 when talking to another player or in box, 0 otherwise
        public override IReadOnlyList<long> UnionWorkPenaltyPointer { get; } = new long[] { 0x4C12CA8, 0xB8, 0x8C }; // 0 when no penalty, float value otherwise.

        public override IReadOnlyList<long> MainSavePointer { get; } = new long[] { 0x4D17AB0, 0x20 };
        public override IReadOnlyList<long> ConfigPointer { get; } = new long[] { 0x4C1DCF8, 0xB8, 0x10, 0xA8 };
    }
}