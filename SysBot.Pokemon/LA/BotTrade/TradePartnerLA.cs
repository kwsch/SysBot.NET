using System;
using System.Diagnostics;
using PKHeX.Core;
using static System.Buffers.Binary.BinaryPrimitives;

namespace SysBot.Pokemon;

public sealed class TradePartnerLA
{
    public string TID7 { get; }
    public string SID7 { get; }
    public string TrainerName { get; }

    public TradePartnerLA(ReadOnlySpan<byte> TIDSID, byte[] trainerNameObject)
    {
        Debug.Assert(TIDSID.Length == 4);
        var tidsid = ReadUInt32LittleEndian(TIDSID);
        TID7 = $"{tidsid % 1_000_000:000000}";
        SID7 = $"{tidsid / 1_000_000:0000}";

        TrainerName = StringConverter8.GetString(trainerNameObject);
    }

    public const int MaxByteLengthStringObject = 26;
}
