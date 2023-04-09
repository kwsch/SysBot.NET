using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using static SysBot.Base.SwitchOffsetType;
using System.Text;

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

        public Task<byte[]> ReadBytesMultiAsync(IReadOnlyDictionary<ulong, int> offsetSizes, CancellationToken token) => Task.Run(() => ReadMulti(offsetSizes, Heap.GetReadMultiMethod(false)), token);
        public Task<byte[]> ReadBytesMainMultiAsync(IReadOnlyDictionary<ulong, int> offsetSizes, CancellationToken token) => Task.Run(() => ReadMulti(offsetSizes, Main.GetReadMultiMethod(false)), token);
        public Task<byte[]> ReadBytesAbsoluteMultiAsync(IReadOnlyDictionary<ulong, int> offsetSizes, CancellationToken token) => Task.Run(() => ReadMulti(offsetSizes, Absolute.GetReadMultiMethod(false)), token);

        public Task WriteBytesAsync(byte[] data, uint offset, CancellationToken token) => Task.Run(() => Write(data, offset, Heap.GetWriteMethod(false)), token);
        public Task WriteBytesMainAsync(byte[] data, ulong offset, CancellationToken token) => Task.Run(() => Write(data, offset, Main.GetWriteMethod(false)), token);
        public Task WriteBytesAbsoluteAsync(byte[] data, ulong offset, CancellationToken token) => Task.Run(() => Write(data, offset, Absolute.GetWriteMethod(false)), token);

        public Task<ulong> GetMainNsoBaseAsync(CancellationToken token)
        {
            return Task.Run(() =>
            {
                Send(SwitchCommand.GetMainNsoBase(false));
                byte[] baseBytes = ReadBulkUSB();
                return BitConverter.ToUInt64(baseBytes, 0);
            }, token);
        }

        public Task<ulong> GetHeapBaseAsync(CancellationToken token)
        {
            return Task.Run(() =>
            {
                Send(SwitchCommand.GetHeapBase(false));
                byte[] baseBytes = ReadBulkUSB();
                return BitConverter.ToUInt64(baseBytes, 0);
            }, token);
        }

        public Task<string> GetTitleID(CancellationToken token)
        {
            return Task.Run(() =>
            {
                Send(SwitchCommand.GetTitleID(false));
                byte[] baseBytes = ReadBulkUSB();
                return BitConverter.ToUInt64(baseBytes, 0).ToString("X16").Trim();
            }, token);
        }

        public Task<string> GetBotbaseVersion(CancellationToken token)
        {
            return Task.Run(() =>
            {
                Send(SwitchCommand.GetBotbaseVersion(false));
                byte[] baseBytes = ReadBulkUSB();
                return Encoding.UTF8.GetString(baseBytes).Trim('\0');
            }, token);
        }

        public Task<string> GetGameInfo(string info, CancellationToken token)
        {
            return Task.Run(() =>
            {
                Send(SwitchCommand.GetGameInfo(info, false));
                byte[] baseBytes = ReadBulkUSB();
                return Encoding.UTF8.GetString(baseBytes).Trim('\0');
            }, token);
        }

        public Task<bool> IsProgramRunning(ulong pid, CancellationToken token)
        {
            return Task.Run(() =>
            {
                Send(SwitchCommand.IsProgramRunning(pid, false));
                byte[] baseBytes = ReadBulkUSB();
                return baseBytes.Length == 1 && BitConverter.ToBoolean(baseBytes, 0);
            }, token);
        }

        public Task<byte[]> ReadRaw(byte[] command, int length, CancellationToken token)
        {
            return Task.Run(() =>
            {
                Send(command);
                return ReadBulkUSB();
            }, token);
        }

        public Task SendRaw(byte[] command, CancellationToken token)
        {
            return Task.Run(() => Send(command), token);
        }

        public Task<byte[]> PointerPeek(int size, IEnumerable<long> jumps, CancellationToken token)
        {
            return Task.Run(() =>
            {
                Send(SwitchCommand.PointerPeek(jumps, size, false));
                return ReadBulkUSB();
            }, token);
        }

        public Task PointerPoke(byte[] data, IEnumerable<long> jumps, CancellationToken token)
        {
            return Task.Run(() => Send(SwitchCommand.PointerPoke(jumps, data, false)), token);
        }

        public Task<ulong> PointerAll(IEnumerable<long> jumps, CancellationToken token)
        {
            return Task.Run(() =>
            {
                Send(SwitchCommand.PointerAll(jumps, false));
                byte[] baseBytes = ReadBulkUSB();
                return BitConverter.ToUInt64(baseBytes, 0);
            }, token);
        }

        public Task<ulong> PointerRelative(IEnumerable<long> jumps, CancellationToken token)
        {
            return Task.Run(() =>
            {
                Send(SwitchCommand.PointerRelative(jumps, false));
                byte[] baseBytes = ReadBulkUSB();
                return BitConverter.ToUInt64(baseBytes, 0);
            }, token);
        }
    }
}