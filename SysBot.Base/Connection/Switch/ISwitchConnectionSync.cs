using System;

namespace SysBot.Base;

/// <summary>
/// Exposes the available interactions for synchronous communications with a Nintendo Switch.
/// </summary>
public interface ISwitchConnectionSync : IConsoleConnectionSync
{
    ulong GetHeapBase();

    ulong GetMainNsoBase();

    byte[] ReadBytesAbsolute(ulong offset, int length);

    byte[] ReadBytesMain(ulong offset, int length);

    void WriteBytesAbsolute(ReadOnlySpan<byte> data, ulong offset);

    void WriteBytesMain(ReadOnlySpan<byte> data, ulong offset);
}
