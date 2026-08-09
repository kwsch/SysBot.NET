using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SysBot.Base;

/// <summary>
/// Exposes the available interactions for asynchronous communications with a Nintendo Switch.
/// </summary>
public interface ISwitchConnectionAsync : IConsoleConnectionAsync
{
    Task<ulong> GetMainNsoBaseAsync(CancellationToken token = default);
    Task<ulong> GetHeapBaseAsync(CancellationToken token = default);
    Task<string> GetTitleID(CancellationToken token = default);
    Task<string> GetBotbaseVersion(CancellationToken token = default);
    Task<string> GetGameInfo(string info, CancellationToken token = default);
    Task<bool> IsProgramRunning(ulong pid, CancellationToken token = default);

    Task<byte[]> ReadBytesMainAsync(ulong offset, int length, CancellationToken token = default);
    Task<byte[]> ReadBytesAbsoluteAsync(ulong offset, int length, CancellationToken token = default);

    Task<byte[]> ReadBytesMultiAsync(IReadOnlyDictionary<ulong, int> offsetSize, CancellationToken token = default);
    Task<byte[]> ReadBytesAbsoluteMultiAsync(IReadOnlyDictionary<ulong, int> offsetSize, CancellationToken token = default);
    Task<byte[]> ReadBytesMainMultiAsync(IReadOnlyDictionary<ulong, int> offsetSize, CancellationToken token = default);

    Task WriteBytesMainAsync(ReadOnlyMemory<byte> data, ulong offset, CancellationToken token = default);
    Task WriteBytesAbsoluteAsync(ReadOnlyMemory<byte> data, ulong offset, CancellationToken token = default);

    Task<byte[]> ReadRaw(ReadOnlyMemory<byte> command, int length, CancellationToken token = default);
    Task SendRaw(ReadOnlyMemory<byte> command, CancellationToken token = default);

    Task<byte[]> PointerPeek(int size, IEnumerable<long> jumps, CancellationToken token = default);
    Task PointerPoke(ReadOnlyMemory<byte> data, IEnumerable<long> jumps, CancellationToken token = default);
    Task<ulong> PointerAll(IEnumerable<long> jumps, CancellationToken token = default);
    Task<ulong> PointerRelative(IEnumerable<long> jumps, CancellationToken token = default);
    Task<(bool Success, T Value)> TryReadMain<T>(ulong offset, CancellationToken token = default) where T : unmanaged;
    Task<(bool Success, T Value)> TryReadAbsolute<T>(ulong offset, CancellationToken token = default) where T : unmanaged;
}
