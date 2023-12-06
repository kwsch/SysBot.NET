using System;
 using static SysBot.Base.SwitchOffsetTypeUtil;

namespace SysBot.Base;

/// <summary>
/// Connection to a Nintendo Switch hosting the sys-module via USB.
/// </summary>
/// <remarks>
/// Interactions are performed synchronously.
/// </remarks>
public sealed class SwitchUSBSync(int Port) : SwitchUSB(Port), ISwitchConnectionSync
{
    public byte[] ReadBytes(uint offset, int length) => Read(Heap, offset, length);
    public byte[] ReadBytesMain(ulong offset, int length) => Read(Main, offset, length);
    public byte[] ReadBytesAbsolute(ulong offset, int length) => Read(Absolute, offset, length);

    public void WriteBytes(ReadOnlySpan<byte> data, uint offset) => Write(Heap, data, offset);
    public void WriteBytesMain(ReadOnlySpan<byte> data, ulong offset) => Write(Main, data, offset);
    public void WriteBytesAbsolute(ReadOnlySpan<byte> data, ulong offset) => Write(Absolute, data, offset);

    public ulong GetMainNsoBase()
    {
        Send(SwitchCommand.GetMainNsoBase(false));
        byte[] baseBytes = ReadBulkUSB();
        return BitConverter.ToUInt64(baseBytes, 0);
    }

    public ulong GetHeapBase()
    {
        Send(SwitchCommand.GetHeapBase(false));
        byte[] baseBytes = ReadBulkUSB();
        return BitConverter.ToUInt64(baseBytes, 0);
    }
}
