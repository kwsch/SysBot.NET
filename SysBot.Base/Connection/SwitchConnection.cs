using System.Threading;

namespace SysBot.Base
{
    /// <summary>
    /// Connection to a Nintendo Switch hosting the sysmodule. 
    /// </summary>
    public class SwitchConnection : SwitchConnectionBase
    {
        public SwitchConnection(string ip, int port) : base(ip, port) { }
        public SwitchConnection(SwitchBotConfig cfg) : this(cfg.IP, cfg.Port) { }

        public void Connect()
        {
            Log("Connecting to device...");
            Connection.Connect(IP, Port);
            Connected = true;
            Log("Connected!");
        }

        public void Disconnect()
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
            var cmd = SwitchCommand.Peek(offset, length);
            Send(cmd);

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
    }
}
