using System.Threading;
using System.Threading.Tasks;

namespace SysBot.Base
{
#pragma warning disable RCS1079 // Throwing of new NotImplementedException.
    public sealed class SwitchUSBAsync : SwitchUSB, ISwitchConnectionAsync
    {
        public SwitchUSBAsync(int port) : base(port)
        {
        }

        public override void Connect()
        {
            throw new System.NotImplementedException();
        }

        public override void Reset()
        {
            throw new System.NotImplementedException();
        }

        public override void Disconnect()
        {
            throw new System.NotImplementedException();
        }

        public Task<int> SendAsync(byte[] buffer, CancellationToken token)
        {
            throw new System.NotImplementedException();
        }

        public Task<byte[]> ReadBytesAsync(uint offset, int length, CancellationToken token)
        {
            throw new System.NotImplementedException();
        }

        public Task WriteBytesAsync(byte[] data, uint offset, CancellationToken token)
        {
            throw new System.NotImplementedException();
        }

        public Task<ulong> GetMainNsoBaseAsync(CancellationToken token)
        {
            throw new System.NotImplementedException();
        }

        public Task<ulong> GetHeapBaseAsync(CancellationToken token)
        {
            throw new System.NotImplementedException();
        }

        public Task<byte[]> ReadBytesMainAsync(ulong offset, int length, CancellationToken token)
        {
            throw new System.NotImplementedException();
        }

        public Task<byte[]> ReadBytesAbsoluteAsync(ulong offset, int length, CancellationToken token)
        {
            throw new System.NotImplementedException();
        }

        public Task WriteBytesMainAsync(byte[] data, ulong offset, CancellationToken token)
        {
            throw new System.NotImplementedException();
        }

        public Task WriteBytesAbsoluteAsync(byte[] data, ulong offset, CancellationToken token)
        {
            throw new System.NotImplementedException();
        }
    }
}