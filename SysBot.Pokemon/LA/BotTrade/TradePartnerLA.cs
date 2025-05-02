using PKHeX.Core;
using System;
using System.Diagnostics;

namespace SysBot.Pokemon;

public sealed class TradePartnerLA
{
    public const int MaxByteLengthStringObject = 0x26;

    public TradePartnerLA(byte[] TIDSID, byte[] trainerNameObject, byte[] idbytes)
    {
        Debug.Assert(TIDSID.Length == 4);
        var tidsid = BitConverter.ToUInt32(TIDSID, 0);
        TID7 = $"{tidsid % 1_000_000:000000}";
        SID7 = $"{tidsid / 1_000_000:0000}";

        TrainerName = StringConverter8.GetString(trainerNameObject);

        Game = idbytes[0];
        Gender = idbytes[1];
        Language = idbytes[3];
    }

    // based on https://github.com/berichan/SysBot.PLA/commit/8196b11a48e66d1ef3fa6c9c8f36c9bcc6cf96e7
    public byte Game { get; }

    public byte Gender { get; }

    public byte Language { get; }

    public ulong NID { get; set; }

    public string SID7 { get; }

    public string TID7 { get; }

    public string TrainerName { get; }
}
