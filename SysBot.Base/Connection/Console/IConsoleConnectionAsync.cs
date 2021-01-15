using System.Threading;
using System.Threading.Tasks;

namespace SysBot.Base
{
    public interface IConsoleConnectionAsync : IConsoleConnection
    {
        Task<int> SendAsync(byte[] buffer, CancellationToken token);

        Task<byte[]> ReadBytesAsync(uint offset, int length, CancellationToken token);
        Task WriteBytesAsync(byte[] data, uint offset, CancellationToken token);
    }
}
