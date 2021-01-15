using System;

namespace SysBot.Base
{
    public enum SwitchOffsetType
    {
        Heap,
        Main,
        Absolute,
    }

    public static class SwitchOffsetTypeExtensions
    {
        public static Func<ulong, int, byte[]> GetReadMethod(this SwitchOffsetType type, bool crlf = true) => type switch
        {
            SwitchOffsetType.Heap => (o, c) => SwitchCommand.Peek((uint)o, c, crlf),
            SwitchOffsetType.Main => (o, c) => SwitchCommand.PeekMain(o, c, crlf),
            SwitchOffsetType.Absolute => (o, c) => SwitchCommand.PeekAbsolute(o, c, crlf),
            _ => throw new IndexOutOfRangeException("Invalid offset type."),
        };

        public static Func<ulong, byte[], byte[]> GetWriteMethod(this SwitchOffsetType type, bool crlf = true) => type switch
        {
            SwitchOffsetType.Heap => (o, b) => SwitchCommand.Poke((uint)o, b, crlf),
            SwitchOffsetType.Main => (o, b) => SwitchCommand.PokeMain(o, b, crlf),
            SwitchOffsetType.Absolute => (o, b) => SwitchCommand.PokeAbsolute(o, b, crlf),
            _ => throw new IndexOutOfRangeException("Invalid offset type."),
        };
    }
}
