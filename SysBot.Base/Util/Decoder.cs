﻿using System;

namespace SysBot.Base
{
    /// <summary>
    /// Decodes a sys-botbase protocol message into raw data.
    /// </summary>
    public static class Decoder
    {
        private static bool IsNum(char c) => (uint)(c - '0') <= 9;
        private static bool IsHexUpper(char c) => (uint)(c - 'A') <= 5;
        private static bool IsHexLower(char c) => (uint)(c - 'a') <= 5;

        public static byte[] ConvertHexByteStringToBytes(byte[] bytes)
        {
            var dest = new byte[bytes.Length / 2];
            for (int i = 0; i < dest.Length; i++)
            {
                int ofs = i * 2;
                var _0 = (char)bytes[ofs + 0];
                var _1 = (char)bytes[ofs + 1];
                dest[i] = DecodeTuple(_0, _1);
            }
            return dest;
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
}
