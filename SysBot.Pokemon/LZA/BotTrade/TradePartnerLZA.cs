using PKHeX.Core;
using System;
using System.Diagnostics;

namespace SysBot.Pokemon;

public sealed class TradePartnerLZA
{
    public ulong NID { get; }
    public string TID7 { get; }
    public string SID7 { get; }
    public string TrainerName { get; }

    public TradePartnerLZA(ulong ID, byte[] TIDSID, byte[] trainerNameObject)
    {
        NID = ID;

        Debug.Assert(TIDSID.Length == 4);
        var tidsid = BitConverter.ToUInt32(TIDSID, 0);
        TID7 = $"{tidsid % 1_000_000:000000}";
        SID7 = $"{tidsid / 1_000_000:0000}";

        TrainerName = StringConverter8.GetString(trainerNameObject);
    }

    public const int MaxByteLengthStringObject = 26;
}
