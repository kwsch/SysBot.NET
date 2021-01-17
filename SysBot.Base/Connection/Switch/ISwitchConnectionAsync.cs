using System.Threading;
using System.Threading.Tasks;

namespace SysBot.Base
{
    /// <summary>
    /// Exposes the available interactions for asynchronous communications with a Nintendo Switch.
    /// </summary>
    public interface ISwitchConnectionAsync : IConsoleConnectionAsync
    {
        Task<ulong> GetMainNsoBaseAsync(CancellationToken token);
        Task<ulong> GetHeapBaseAsync(CancellationToken token);

        Task<byte[]> ReadBytesMainAsync(ulong offset, int length, CancellationToken token);
        Task<byte[]> ReadBytesAbsoluteAsync(ulong offset, int length, CancellationToken token);

        Task WriteBytesMainAsync(byte[] data, ulong offset, CancellationToken token);
        Task WriteBytesAbsoluteAsync(byte[] data, ulong offset, CancellationToken token);
    }
}
