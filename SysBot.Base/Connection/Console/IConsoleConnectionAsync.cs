using System;
using System.Threading;
using System.Threading.Tasks;

namespace SysBot.Base;

/// <summary>
/// Bare minimum methods required to interact with a <see cref="IConsoleConnection"/> in an asynchronous manner.
/// </summary>
public interface IConsoleConnectionAsync : IConsoleConnection
{
    ValueTask<int> SendAsync(ReadOnlyMemory<byte> buffer, CancellationToken token = default);

    Task<byte[]> ReadBytesAsync(uint offset, int length, CancellationToken token = default);
    Task WriteBytesAsync(ReadOnlyMemory<byte> data, uint offset, CancellationToken token = default);
}
