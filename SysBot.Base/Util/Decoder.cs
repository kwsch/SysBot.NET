using System;

namespace SysBot.Base;

/// <summary>
/// Decodes a sys-botbase protocol message into raw data.
/// </summary>
public static class Decoder
{
    private static bool IsNum(char c) => (uint)(c - '0') <= 9;
    private static bool IsHexUpper(char c) => (uint)(c - 'A') <= 5;
    private static bool IsHexLower(char c) => (uint)(c - 'a') <= 5;

    public static byte[] ConvertHexByteStringToBytes(ReadOnlySpan<byte> bytes)
    {
        var dest = new byte[bytes.Length / 2];
        LoadHexBytesTo(bytes, dest, 2);
        return dest;
    }

    public static void LoadHexBytesTo(ReadOnlySpan<byte> str, Span<byte> dest, int tupleSize)
    {
        // The input string is 2-char hex values optionally separated.
        // The destination array should always be larger or equal than the bytes written. Let the runtime bounds check us.
        // Iterate through the string without allocating.
        for (int i = 0, j = 0; i < str.Length; i += tupleSize)
            dest[j++] = DecodeTuple((char)str[i + 0], (char)str[i + 1]);
    }

    private static byte DecodeTuple(char _0, char _1)
    {
        byte result;
        if (IsNum(_0))
            result = (byte)((_0 - '0') << 4);
        else if (IsHexUpper(_0))
            result = (byte)((_0 - 'A' + 10) << 4);
        else if (IsHexLower(_0))
            result = (byte)((_0 - 'a' + 10) << 4);
        else
            throw new ArgumentOutOfRangeException(nameof(_0));

        if (IsNum(_1))
            result |= (byte)(_1 - '0');
        else if (IsHexUpper(_1))
            result |= (byte)(_1 - 'A' + 10);
        else if (IsHexLower(_1))
            result |= (byte)(_1 - 'a' + 10);
        else
            throw new ArgumentOutOfRangeException(nameof(_1));
        return result;
    }
}
