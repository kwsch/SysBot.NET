using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using NLog;

namespace SysBot.Base
{
    /// <summary>
    /// Connection to a Nintendo Switch hosting the sysmodule. 
    /// </summary>
    public class SwitchBot
    {
        public readonly Socket Connection = new Socket(SocketType.Stream, ProtocolType.IPv4);
        private readonly string IP;
        private readonly int Port;

        public string Name { get; set; }

        public SwitchBot(string ipaddress, int port)
        {
            IP = ipaddress;
            Port = port;
            Name = $"Unnamed Bot: {GetType().Name}";
            LogUtil.Log(LogLevel.Info, "I'm Alive!", Name);
        }

        public SwitchBot(SwitchBotConfig cfg) : this(cfg.IP, cfg.Port) { }

        public void Log(string message, LogLevel level) => LogUtil.Log(level, message, Name);
        public void Log(string message) => Log(message, LogLevel.Info);
        public void LogError(string message) => Log(message, LogLevel.Error);

        public async Task Connect() => await Connection.ConnectAsync(IP, Port).ConfigureAwait(false);
        public async Task<bool> Disconnect() => await Task.Run(() => Connection.DisconnectAsync(new SocketAsyncEventArgs())).ConfigureAwait(false);
        public async Task<int> Read(byte[] buffer, CancellationToken token) => await Task.Run(() => Connection.Receive(buffer), token).ConfigureAwait(false);
        public async Task<int> Send(byte[] buffer, CancellationToken token) => await Task.Run(() => Connection.Send(buffer), token).ConfigureAwait(false);

        public async Task<byte[]> ReadBytes(uint myGiftAddress, int length, CancellationToken token)
        {
            var cmd = SwitchCommand.Peek(myGiftAddress, length);
            await Send(cmd, token).ConfigureAwait(false);

            // give it time to push data back
            await Task.Delay((length / 8) + 200, token).ConfigureAwait(false);
            var buffer = new byte[(length * 2) + 1];
            var _ = await Read(buffer, token).ConfigureAwait(false);
            return Decoder.ConvertHexByteStringToBytes(buffer);
        }
    }
}