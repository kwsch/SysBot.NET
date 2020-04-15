using System;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace SysBot.Base
{
    /// <summary>
    /// Connection to a Nintendo Switch hosting the sys-module.
    /// </summary>
    public class SwitchConnectionAsync : SwitchConnectionBase
    {
        public SwitchConnectionAsync(string ipaddress, int port) : base(ipaddress, port) { }
        public SwitchConnectionAsync(SwitchBotConfig cfg) : this(cfg.IP, cfg.Port) { }

        public void Connect()
        {
            if (Connected)
            {
                Log("Already connected prior, skipping initial connection.");
                return;
            }

            Log("Connecting to device...");
            Connection.Connect(IP, Port);
            Connected = true;
            Log("Connected!");
        }

        public void Disconnect()
        {
            Log("Disconnecting from device...");
            Connection.Shutdown(SocketShutdown.Both);
            Connection.BeginDisconnect(true, DisconnectCallback, Connection);
            Connected = false;
            Log("Disconnected!");
        }

        private readonly AutoResetEvent disconnectDone = new AutoResetEvent(false);

        private void DisconnectCallback(IAsyncResult ar)
        {
            // Complete the disconnect request.
            Socket client = (Socket)ar.AsyncState;
            client.EndDisconnect(ar);

            // Signal that the disconnect is complete.
            disconnectDone.Set();
            LogUtil.LogInfo("Disconnected.", Name);
        }

        public int Read(byte[] buffer)
        {
            int br = Connection.Receive(buffer, 0, 1, SocketFlags.None);
            while (buffer[br - 1] != (byte)'\n')
                br += Connection.Receive(buffer, br, 1, SocketFlags.None);
            return br;
        }

        public async Task<int> SendAsync(byte[] buffer, CancellationToken token) => await Task.Run(() => Connection.Send(buffer), token).ConfigureAwait(false);

        private async Task<byte[]> ReadBytesFromCmdAsync(byte[] cmd, int length, CancellationToken token)
        {
            await SendAsync(cmd, token).ConfigureAwait(false);

            var buffer = new byte[(length * 2) + 1];
            var _ = Read(buffer);
            return Decoder.ConvertHexByteStringToBytes(buffer);
        }

        public async Task<byte[]> ReadBytesAsync(uint offset, int length, CancellationToken token)
        {
            return await ReadBytesFromCmdAsync(SwitchCommand.Peek(offset, length), length, token).ConfigureAwait(false);
        }

        public async Task<byte[]> ReadBytesAbsoluteAsync(ulong offset, int length, CancellationToken token)
        {
            return await ReadBytesFromCmdAsync(SwitchCommand.PeekAbsolute(offset, length), length, token).ConfigureAwait(false);
        }

        public async Task<ulong> GetMainNsoBaseAsync(CancellationToken token)
        {
            byte[] baseBytes = await ReadBytesFromCmdAsync(SwitchCommand.GetMainNsoBase(), sizeof(ulong), token).ConfigureAwait(false);
            Array.Reverse(baseBytes, 0, 8);
            return BitConverter.ToUInt64(baseBytes, 0);
        }

        public async Task<ulong> GetHeapBaseAsync(CancellationToken token)
        {
            var baseBytes = await ReadBytesFromCmdAsync(SwitchCommand.GetHeapBase(), sizeof(ulong), token).ConfigureAwait(false);
            Array.Reverse(baseBytes, 0, 8);
            return BitConverter.ToUInt64(baseBytes, 0);
        }

        public async Task WriteBytesAsync(byte[] data, uint offset, CancellationToken token)
        {
            var cmd = SwitchCommand.Poke(offset, data);
            await SendAsync(cmd, token).ConfigureAwait(false);
        }

        public async Task WriteBytesAbsoluteAsync(byte[] data, ulong offset, CancellationToken token)
        {
            var cmd = SwitchCommand.PokeAbsolute(offset, data);
            await SendAsync(cmd, token).ConfigureAwait(false);
        }
    }
}
