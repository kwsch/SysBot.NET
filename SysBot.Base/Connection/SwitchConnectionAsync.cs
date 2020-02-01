using System;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using NLog;

namespace SysBot.Base
{
    /// <summary>
    /// Connection to a Nintendo Switch hosting the sysmodule. 
    /// </summary>
    public class SwitchConnectionAsync : SwitchConnectionBase
    {
        public SwitchConnectionAsync(string ipaddress, int port) : base(ipaddress, port) { }
        public SwitchConnectionAsync(SwitchBotConfig cfg) : this(cfg.IP, cfg.Port) { }

        public async Task Connect()
        {
            await Connection.ConnectAsync(IP, Port).ConfigureAwait(false);
            Connected = true;
        }

        public void Disconnect()
        {
            Connection.Shutdown(SocketShutdown.Both);
            Connection.BeginDisconnect(true, DisconnectCallback, Connection);
            Connected = false;
        }

        private readonly AutoResetEvent disconnectDone = new AutoResetEvent(false);

        private void DisconnectCallback(IAsyncResult ar)
        {
            // Complete the disconnect request.
            Socket client = (Socket)ar.AsyncState;
            client.EndDisconnect(ar);

            // Signal that the disconnect is complete.
            disconnectDone.Set();
            LogUtil.Log(LogLevel.Info, "Disconnected.", Name);
        }

        public async Task<int> ReadAsync(byte[] buffer, CancellationToken token) => await Task.Run(() => Connection.Receive(buffer), token).ConfigureAwait(false);
        public async Task<int> SendAsync(byte[] buffer, CancellationToken token) => await Task.Run(() => Connection.Send(buffer), token).ConfigureAwait(false);

        private const int BaseDelay = 64;
        private const int DelayFactor = 256;

        public async Task<byte[]> ReadBytesAsync(uint offset, int length, CancellationToken token)
        {
            var cmd = SwitchCommand.Peek(offset, length);
            await SendAsync(cmd, token).ConfigureAwait(false);

            // give it time to push data back
            await Task.Delay((length / DelayFactor) + BaseDelay, token).ConfigureAwait(false);
            var buffer = new byte[(length * 2) + 1];
            var _ = await ReadAsync(buffer, token).ConfigureAwait(false);
            return Decoder.ConvertHexByteStringToBytes(buffer);
        }

        public async Task WriteBytesAsync(byte[] data, uint offset, CancellationToken token)
        {
            var cmd = SwitchCommand.Poke(offset, data);
            await SendAsync(cmd, token).ConfigureAwait(false);

            // give it time to push data back
            await Task.Delay((data.Length / DelayFactor) + BaseDelay, token).ConfigureAwait(false);
        }
    }
}