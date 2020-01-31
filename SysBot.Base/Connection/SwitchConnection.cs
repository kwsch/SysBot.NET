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
            Connection.Connect(IP, Port);
            Connected = true;
        }

        public void Disconnect()
        {
            Connection.Disconnect(false);
            Connected = false;
        }

        public int Read(byte[] buffer) => Connection.Receive(buffer);
        public int Send(byte[] buffer) => Connection.Send(buffer);

        private const int BaseDelay = 200;
        private const int DelayFactor = 8;

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