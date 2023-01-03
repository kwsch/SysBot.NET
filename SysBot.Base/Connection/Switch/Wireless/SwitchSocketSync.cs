using System;
using System.Threading;
using static SysBot.Base.SwitchOffsetType;

namespace SysBot.Base
{
    /// <summary>
    /// Connection to a Nintendo Switch hosting the sys-module via a socket (WiFi).
    /// </summary>
    /// <remarks>
    /// Interactions are performed synchronously.
    /// </remarks>
    public sealed class SwitchSocketSync : SwitchSocket, ISwitchConnectionSync
    {
        public SwitchSocketSync(IWirelessConnectionConfig cfg) : base(cfg) { }

        public override void Connect()
        {
            Log("Connecting to device...");
            Connection.Connect(Info.IP, Info.Port);
            Log("Connected!");
        }

        public override void Reset()
        {
            Disconnect();
            Connect();
        }

        public override void Disconnect()
        {
            Log("Disconnecting from device...");
            Connection.Disconnect(false);
            Log("Disconnected!");
        }

        private int Read(byte[] buffer) => Connection.Receive(buffer);
        public int Send(byte[] buffer) => Connection.Send(buffer);

        private byte[] ReadResponse(int length)
        {
            // give it time to push data back
            Thread.Sleep((MaximumTransferSize / DelayFactor) + BaseDelay);
            var buffer = new byte[(length * 2) + 1];
            var _ = Read(buffer);
            return Decoder.ConvertHexByteStringToBytes(buffer);
        }

        public ulong GetMainNsoBase()
        {
            Send(SwitchCommand.GetMainNsoBase());
            byte[] baseBytes = ReadResponse(8);
            Array.Reverse(baseBytes, 0, 8);
            return BitConverter.ToUInt64(baseBytes, 0);
        }

        public ulong GetHeapBase()
        {
            Send(SwitchCommand.GetHeapBase());
            byte[] baseBytes = ReadResponse(8);
            Array.Reverse(baseBytes, 0, 8);
            return BitConverter.ToUInt64(baseBytes, 0);
        }

        public byte[] ReadBytes(uint offset, int length) => Read(offset, length, Heap);
        public byte[] ReadBytesMain(ulong offset, int length) => Read(offset, length, Main);
        public byte[] ReadBytesAbsolute(ulong offset, int length) => Read(offset, length, Absolute);

        public void WriteBytes(byte[] data, uint offset) => Write(data, offset, Heap);
        public void WriteBytesMain(byte[] data, ulong offset) => Write(data, offset, Main);
        public void WriteBytesAbsolute(byte[] data, ulong offset) => Write(data, offset, Absolute);

        private byte[] Read(ulong offset, int length, SwitchOffsetType type)
        {
            var method = type.GetReadMethod();
            if (length <= MaximumTransferSize)
            {
                var cmd = method(offset, length);
                Send(cmd);
                return ReadResponse(length);
            }

            byte[] result = new byte[length];
            for (int i = 0; i < length; i += MaximumTransferSize)
            {
                int len = MaximumTransferSize;
                int delta = length - i;
                if (delta < MaximumTransferSize)
                    len = delta;

                var cmd = method(offset + (uint)i, len);
                Send(cmd);
                var bytes = ReadResponse(len);
                bytes.CopyTo(result, i);
            }
            return result;
        }

        private void Write(byte[] data, ulong offset, SwitchOffsetType type)
        {
            var method = type.GetWriteMethod();
            if (data.Length <= MaximumTransferSize)
            {
                var cmd = method(offset, data);
                Send(cmd);
                return;
            }
            int byteCount = data.Length;
            for (int i = 0; i < byteCount; i += MaximumTransferSize)
            {
                var slice = data.SliceSafe(i, MaximumTransferSize);
                var cmd = method(offset + (uint)i, slice);
                Send(cmd);
                Thread.Sleep((MaximumTransferSize / DelayFactor) + BaseDelay);
            }
        }
    }
}
