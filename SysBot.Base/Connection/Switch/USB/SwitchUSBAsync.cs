using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using static SysBot.Base.SwitchOffsetType;

namespace SysBot.Base
{
    /// <summary>
    /// Connection to a Nintendo Switch hosting the sys-module via USB.
    /// </summary>
    /// <remarks>
    /// Interactions are performed asynchronously.
    /// </remarks>
    public sealed class SwitchUSBAsync : SwitchUSB, ISwitchConnectionAsync
    {
        public SwitchUSBAsync(int port) : base(port)
        {
        }

        public Task<int> SendAsync(byte[] data, CancellationToken token)
        {
            Debug.Assert(data.Length < MaximumTransferSize);
            return Task.Run(() => Send(data), token);
        }

        public Task<byte[]> ReadBytesAsync(uint offset, int length, CancellationToken token) => Task.Run(() => Read(offset, length, Heap.GetReadMethod(false)), token);
        public Task<byte[]> ReadBytesMainAsync(ulong offset, int length, CancellationToken token) => Task.Run(() => Read(offset, length, Main.GetReadMethod(false)), token);
        public Task<byte[]> ReadBytesAbsoluteAsync(ulong offset, int length, CancellationToken token) => Task.Run(() => Read(offset, length, Absolute.GetReadMethod(false)), token);

        public Task WriteBytesAsync(byte[] data, uint offset, CancellationToken token) => Task.Run(() => Write(data, offset, Heap.GetWriteMethod(false)), token);
        public Task WriteBytesMainAsync(byte[] data, ulong offset, CancellationToken token) => Task.Run(() => Write(data, offset, Main.GetWriteMethod(false)), token);
        public Task WriteBytesAbsoluteAsync(byte[] data, ulong offset, CancellationToken token) => Task.Run(() => Write(data, offset, Absolute.GetWriteMethod(false)), token);

        public Task<ulong> GetMainNsoBaseAsync(CancellationToken token)
        {
            return Task.Run(() =>
            {
                Send(SwitchCommand.GetMainNsoBase(false));
                byte[] baseBytes = ReadResponse(8);
                Array.Reverse(baseBytes, 0, 8);
                return BitConverter.ToUInt64(baseBytes, 0);
            }, token);
        }

        public Task<ulong> GetHeapBaseAsync(CancellationToken token)
        {
            return Task.Run(() =>
            {
                Send(SwitchCommand.GetHeapBase(false));
                byte[] baseBytes = ReadResponse(8);
                Array.Reverse(baseBytes, 0, 8);
                return BitConverter.ToUInt64(baseBytes, 0);
            }, token);
        }
    }
}
