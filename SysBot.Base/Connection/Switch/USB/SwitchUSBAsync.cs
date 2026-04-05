using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static SysBot.Base.SwitchOffsetTypeUtil;

namespace SysBot.Base;

/// <summary>
/// Connection to a Nintendo Switch hosting the sys-module via USB.
/// </summary>
/// <remarks>
/// Interactions are performed asynchronously.
/// </remarks>
public sealed class SwitchUSBAsync(int Port) : SwitchUSB(Port), ISwitchConnectionAsync
{
    public ValueTask<int> SendAsync(byte[] data, CancellationToken token)
    {
        Debug.Assert(data.Length < MaximumTransferSize);
        var res = Task.Run(() => Send(data), token);
        return new ValueTask<int>(res);
    }

    public Task<byte[]> ReadBytesAsync(uint offset, int length, CancellationToken token) => Task.Run(() => Read(Heap, offset, length), token);
    public Task<byte[]> ReadBytesMainAsync(ulong offset, int length, CancellationToken token) => Task.Run(() => Read(Main, offset, length), token);
    public Task<byte[]> ReadBytesAbsoluteAsync(ulong offset, int length, CancellationToken token) => Task.Run(() => Read(Absolute, offset, length), token);

    public Task<byte[]> ReadBytesMultiAsync(IReadOnlyDictionary<ulong, int> offsetSizes, CancellationToken token) => Task.Run(() => ReadMulti(Heap, offsetSizes), token);
    public Task<byte[]> ReadBytesMainMultiAsync(IReadOnlyDictionary<ulong, int> offsetSizes, CancellationToken token) => Task.Run(() => ReadMulti(Main, offsetSizes), token);
    public Task<byte[]> ReadBytesAbsoluteMultiAsync(IReadOnlyDictionary<ulong, int> offsetSizes, CancellationToken token) => Task.Run(() => ReadMulti(Absolute, offsetSizes), token);

    public Task WriteBytesAsync(byte[] data, uint offset, CancellationToken token) => Task.Run(() => Write(Heap, data, offset), token);

    public Task WriteBytesMainAsync(Span<byte> data, ulong offset, CancellationToken token)
    {
        var arr = data.ToArray();
        return Task.Run(() => Write(Main, arr, offset), token);
    }

    public Task WriteBytesAbsoluteAsync(Span<byte> data, ulong offset, CancellationToken token)
    {
        var arr = data.ToArray();
        return Task.Run(() => Write(Absolute, arr, offset), token);
    }

    public Task<ulong> GetMainNsoBaseAsync(CancellationToken token)
    {
        return Task.Run<ulong>(() =>
        {
            Send(SwitchCommand.GetMainNsoBase(false));
            byte[] baseBytes = ReadBulkUSB();
            if (baseBytes.Length < sizeof(ulong))
            {
                Log($"{nameof(GetMainNsoBaseAsync)}: Invalid response length");
                return 0;
            }
            return BitConverter.ToUInt64(baseBytes, 0);
        }, token);
    }

    public Task<ulong> GetHeapBaseAsync(CancellationToken token)
    {
        return Task.Run<ulong>(() =>
        {
            Send(SwitchCommand.GetHeapBase(false));
            byte[] baseBytes = ReadBulkUSB();
            if (baseBytes.Length < sizeof(ulong))
            {
                Log($"{nameof(GetHeapBaseAsync)}: Invalid response length");
                return 0;
            }
            return BitConverter.ToUInt64(baseBytes, 0);
        }, token);
    }

    public Task<string> GetTitleID(CancellationToken token)
    {
        return Task.Run<string>(() =>
        {
            Send(SwitchCommand.GetTitleID(false));
            byte[] baseBytes = ReadBulkUSB();
            if (baseBytes.Length == 0)
            {
                Log($"{nameof(GetTitleID)}: Invalid response");
                return string.Empty;
            }
            return BitConverter.ToUInt64(baseBytes, 0).ToString("X16").Trim();
        }, token);
    }

    public Task<string> GetBotbaseVersion(CancellationToken token)
    {
        return Task.Run<string>(() =>
        {
            Send(SwitchCommand.GetBotbaseVersion(false));
            byte[] baseBytes = ReadBulkUSB();
            if (baseBytes.Length == 0)
            {
                Log($"{nameof(GetBotbaseVersion)}: Invalid response");
                return string.Empty;
            }
            return Encoding.UTF8.GetString(baseBytes).Trim('\0');
        }, token);
    }

    public Task<string> GetGameInfo(string info, CancellationToken token)
    {
        return Task.Run<string>(() =>
        {
            Send(SwitchCommand.GetGameInfo(info, false));
            byte[] baseBytes = ReadBulkUSB();
            if (baseBytes.Length == 0)
            {
                Log($"{nameof(GetGameInfo)}: Invalid response");
                return string.Empty;
            }
            return Encoding.UTF8.GetString(baseBytes).Trim('\0');
        }, token);
    }

    public Task<bool> IsProgramRunning(ulong pid, CancellationToken token)
    {
        return Task.Run<bool>(() =>
        {
            Send(SwitchCommand.IsProgramRunning(pid, false));
            byte[] baseBytes = ReadBulkUSB();
            if (baseBytes.Length == 0)
            {
                Log($"{nameof(IsProgramRunning)}: Invalid response");
                return false;
            }
            return baseBytes.Length == 1 && BitConverter.ToBoolean(baseBytes, 0);
        }, token);
    }

    public Task<byte[]> ReadRaw(byte[] command, int length, CancellationToken token)
    {
        return Task.Run(() =>
        {
            Send(command);
            return ReadBulkUSB();
        }, token);
    }

    public Task SendRaw(byte[] command, CancellationToken token)
    {
        return Task.Run(() => Send(command), token);
    }

    public Task<byte[]> PointerPeek(int size, IEnumerable<long> jumps, CancellationToken token)
    {
        return Task.Run(() =>
        {
            Send(SwitchCommand.PointerPeek(jumps, size, false));
            return ReadBulkUSB();
        }, token);
    }

    public Task PointerPoke(byte[] data, IEnumerable<long> jumps, CancellationToken token)
    {
        return Task.Run(() => Send(SwitchCommand.PointerPoke(jumps, data, false)), token);
    }

    public Task<ulong> PointerAll(IEnumerable<long> jumps, CancellationToken token)
    {
        return Task.Run<ulong>(() =>
        {
            Send(SwitchCommand.PointerAll(jumps, false));
            byte[] baseBytes = ReadBulkUSB();
            if (baseBytes.Length < sizeof(ulong))
            {
                Log($"{nameof(PointerAll)}: Invalid response length {baseBytes?.Length ?? 0}");
                return 0;
            }
            return BitConverter.ToUInt64(baseBytes, 0);
        }, token);
    }

    public Task<ulong> PointerRelative(IEnumerable<long> jumps, CancellationToken token)
    {
        return Task.Run<ulong>(() =>
        {
            Send(SwitchCommand.PointerRelative(jumps, false));
            byte[] baseBytes = ReadBulkUSB();
            if (baseBytes.Length < sizeof(ulong))
            {
                Log($"{nameof(PointerRelative)}: Invalid response length {baseBytes?.Length ?? 0}");
                return 0;
            }
            return BitConverter.ToUInt64(baseBytes, 0);
        }, token);
    }

    private async Task<(bool, T)> TryReadInternal<T>(Func<ulong, int, CancellationToken, Task<byte[]>> readMethod, ulong offset, CancellationToken token) where T : unmanaged
    {
        try
        {
            int size = Marshal.SizeOf<T>();
            var data = await readMethod(offset, size, token).ConfigureAwait(false);
            if (data.Length < size)
                return (false, default);

            if (!BitConverter.IsLittleEndian)
                Array.Reverse(data, 0, size);

            T value = MemoryMarshal.Read<T>(data);
            return (true, value);
        }
        catch (Exception ex)
        {
            Log($"{nameof(TryReadInternal)}<{typeof(T).Name}> failed: {ex.Message}");
            return (false, default);
        }
    }

    public Task<(bool Success, T Value)> TryReadMain<T>(ulong offset, CancellationToken token) where T : unmanaged
        => TryReadInternal<T>(ReadBytesMainAsync, offset, token);

    public Task<(bool Success, T Value)> TryReadAbsolute<T>(ulong offset, CancellationToken token) where T : unmanaged
        => TryReadInternal<T>(ReadBytesAbsoluteAsync, offset, token);
}
