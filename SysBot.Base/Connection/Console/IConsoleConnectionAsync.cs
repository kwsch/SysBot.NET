using System.Threading;
using System.Threading.Tasks;

namespace SysBot.Base;

/// <summary>
/// Bare minimum methods required to interact with a <see cref="IConsoleConnection"/> in an asynchronous manner.
/// </summary>
public interface IConsoleConnectionAsync : IConsoleConnection
{
    Task<byte[]> PixelPeek(CancellationToken token);

    Task<byte[]> ReadBytesAsync(uint offset, int length, CancellationToken token);

    ValueTask<int> SendAsync(byte[] buffer, CancellationToken token);

    Task WriteBytesAsync(byte[] data, uint offset, CancellationToken token);
}
