using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace SysBot.Base
{
    /// <summary>
    /// Exposes the available interactions for asynchronous communications with a Nintendo Switch.
    /// </summary>
    public interface ISwitchConnectionAsync : IConsoleConnectionAsync
    {
        Task<ulong> GetMainNsoBaseAsync(CancellationToken token);
        Task<ulong> GetHeapBaseAsync(CancellationToken token);
        Task<string> GetTitleID(CancellationToken token);
        Task<string> GetBotbaseVersion(CancellationToken token);
        Task<string> GetGameInfo(string info, CancellationToken token);
        Task<bool> IsProgramRunning(ulong pid, CancellationToken token);

        Task<byte[]> ReadBytesMainAsync(ulong offset, int length, CancellationToken token);
        Task<byte[]> ReadBytesAbsoluteAsync(ulong offset, int length, CancellationToken token);

        Task<byte[]> ReadBytesMultiAsync(IReadOnlyDictionary<ulong, int> offsetSize, CancellationToken token);
        Task<byte[]> ReadBytesAbsoluteMultiAsync(IReadOnlyDictionary<ulong, int> offsetSize, CancellationToken token);
        Task<byte[]> ReadBytesMainMultiAsync(IReadOnlyDictionary<ulong, int> offsetSize, CancellationToken token);

        Task WriteBytesMainAsync(byte[] data, ulong offset, CancellationToken token);
        Task WriteBytesAbsoluteAsync(byte[] data, ulong offset, CancellationToken token);

        Task<byte[]> ReadRaw(byte[] command, int length, CancellationToken token);
        Task SendRaw(byte[] command, CancellationToken token);

        Task<byte[]> PointerPeek(int size, IEnumerable<long> jumps, CancellationToken token);
        Task PointerPoke(byte[] data, IEnumerable<long> jumps, CancellationToken token);
        Task<ulong> PointerAll(IEnumerable<long> jumps, CancellationToken token);
        Task<ulong> PointerRelative(IEnumerable<long> jumps, CancellationToken token);
    }
}