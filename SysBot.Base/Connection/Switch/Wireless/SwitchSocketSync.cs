using System;
using System.Threading;

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
            Connected = true;
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
            Connected = false;
            Log("Disconnected!");
        }

        public int Read(byte[] buffer) => Connection.Receive(buffer);
        public int Send(byte[] buffer) => Connection.Send(buffer);

        private const int BaseDelay = 64;
        private const int DelayFactor = 256;

        public byte[] ReadBytes(uint offset, int length)
        {
            Send(SwitchCommand.Peek(offset, length));
            return ReadResponse(length);
        }

        private byte[] ReadResponse(int length)
        {
            // give it time to push data back
            Thread.Sleep((length / DelayFactor) + BaseDelay);
            var buffer = new byte[(length * 2) + 1];
            var _ = Read(buffer);
            return Decoder.ConvertHexByteStringToBytes(buffer);
        }

        public void WriteBytes(byte[] data, uint offset)
        {
            Send(SwitchCommand.Poke(offset, data));
            // give it time to push data back
            Thread.Sleep((data.Length / DelayFactor) + BaseDelay);
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

        public byte[] ReadBytesMain(ulong offset, int length)
        {
            Send(SwitchCommand.PeekMain(offset, length));
            return ReadResponse(length);
        }

        public byte[] ReadBytesAbsolute(ulong offset, int length)
        {
            Send(SwitchCommand.PeekAbsolute(offset, length));
            return ReadResponse(length);
        }

        public void WriteBytesMain(byte[] data, ulong offset)
        {
            Send(SwitchCommand.PokeMain(offset, data));
        }

        public void WriteBytesAbsolute(byte[] data, ulong offset)
        {
            Send(SwitchCommand.PokeAbsolute(offset, data));
        }
    }
}
