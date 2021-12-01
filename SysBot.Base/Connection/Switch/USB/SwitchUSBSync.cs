using System;
using static SysBot.Base.SwitchOffsetType;

namespace SysBot.Base
{
    /// <summary>
    /// Connection to a Nintendo Switch hosting the sys-module via USB.
    /// </summary>
    /// <remarks>
    /// Interactions are performed synchronously.
    /// </remarks>
    public sealed class SwitchUSBSync : SwitchUSB, ISwitchConnectionSync
    {
        public SwitchUSBSync(int port) : base(port)
        {
        }

        public byte[] ReadBytes(uint offset, int length) => Read(offset, length, Heap.GetReadMethod(false));
        public byte[] ReadBytesMain(ulong offset, int length) => Read(offset, length, Main.GetReadMethod(false));
        public byte[] ReadBytesAbsolute(ulong offset, int length) => Read(offset, length, Absolute.GetReadMethod(false));

        public void WriteBytes(byte[] data, uint offset) => Write(data, offset, Heap.GetWriteMethod(false));
        public void WriteBytesMain(byte[] data, ulong offset) => Write(data, offset, Main.GetWriteMethod(false));
        public void WriteBytesAbsolute(byte[] data, ulong offset) => Write(data, offset, Absolute.GetWriteMethod(false));

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
}