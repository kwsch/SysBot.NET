using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static SysBot.Base.SwitchOffsetTypeUtil;

namespace SysBot.Base;

/// <summary>
/// Connection to a Nintendo Switch hosting the sys-module via a socket (Wi-Fi).
/// </summary>
/// <remarks>
/// Interactions are performed asynchronously.
/// </remarks>
public sealed class SwitchSocketAsync : SwitchSocket, ISwitchConnectionAsync
{
    private SwitchSocketAsync(IWirelessConnectionConfig cfg) : base(cfg) { }

    public static SwitchSocketAsync CreateInstance(IWirelessConnectionConfig cfg)
    {
        return new SwitchSocketAsync(cfg);
    }

    public override void Connect()
    {
        if (Connected)
        {
            Log("Already connected prior, skipping initial connection.");
            return;
        }

        Log("Connecting to device...");
        IAsyncResult result = Connection.BeginConnect(Info.IP, Info.Port, null, null);
        bool success = result.AsyncWaitHandle.WaitOne(5000, true);
        if (!success || !Connection.Connected)
        {
            InitializeSocket();
            throw new Exception("Failed to connect to device.");
        }
        Connection.EndConnect(result);
        Log("Connected!");
        Label = Name;
    }

    public override void Reset()
    {
        if (Connected)
            Disconnect();
        else
            InitializeSocket();
        Connect();
    }

    public override void Disconnect()
    {
        Log("Disconnecting from device...");
        IAsyncResult result = Connection.BeginDisconnect(false, null, null);
        bool success = result.AsyncWaitHandle.WaitOne(5000, true);
        if (!success || Connection.Connected)
        {
            InitializeSocket();
            throw new Exception("Failed to disconnect from device.");
        }
        Connection.EndDisconnect(result);
        Log("Disconnected! Resetting Socket.");
        InitializeSocket();
    }

    /// <summary> Only call this if you are sending small commands. </summary>
    public ValueTask<int> SendAsync(byte[] buffer, CancellationToken token) => Connection.SendAsync(buffer, token);

    private async Task<byte[]> ReadBytesFromCmdAsync(byte[] cmd, int length, CancellationToken token)
    {
        await SendAsync(cmd, token).ConfigureAwait(false);
        var size = (length * 2) + 1;
        var buffer = ArrayPool<byte>.Shared.Rent(size);
        var mem = buffer.AsMemory()[..size];
        await Connection.ReceiveAsync(mem, token);
        var result = DecodeResult(mem, length);
        ArrayPool<byte>.Shared.Return(buffer, true);
        return result;
    }

    private static byte[] DecodeResult(ReadOnlyMemory<byte> buffer, int length)
    {
        var result = new byte[length];
        var span = buffer.Span[..^1]; // Last byte is always a terminator
        Decoder.LoadHexBytesTo(span, result, 2);
        return result;
    }

    public Task<byte[]> ReadBytesAsync(uint offset, int length, CancellationToken token) => Read(Heap, offset, length, token);
    public Task<byte[]> ReadBytesMainAsync(ulong offset, int length, CancellationToken token) => Read(Main, offset, length, token);
    public Task<byte[]> ReadBytesAbsoluteAsync(ulong offset, int length, CancellationToken token) => Read(Absolute, offset, length, token);

    public Task<byte[]> ReadBytesMultiAsync(IReadOnlyDictionary<ulong, int> offsetSizes, CancellationToken token) => ReadMulti(Heap, offsetSizes, token);
    public Task<byte[]> ReadBytesMainMultiAsync(IReadOnlyDictionary<ulong, int> offsetSizes, CancellationToken token) => ReadMulti(Main, offsetSizes, token);
    public Task<byte[]> ReadBytesAbsoluteMultiAsync(IReadOnlyDictionary<ulong, int> offsetSizes, CancellationToken token) => ReadMulti(Absolute, offsetSizes, token);

    public Task WriteBytesAsync(byte[] data, uint offset, CancellationToken token) => Write(Heap, data, offset, token);
    public Task WriteBytesMainAsync(byte[] data, ulong offset, CancellationToken token) => Write(Main, data, offset, token);
    public Task WriteBytesAbsoluteAsync(byte[] data, ulong offset, CancellationToken token) => Write(Absolute, data, offset, token);

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

    public async Task<string> GetTitleID(CancellationToken token)
    {
        var bytes = await ReadRaw(SwitchCommand.GetTitleID(), 17, token).ConfigureAwait(false);
        return Encoding.ASCII.GetString(bytes).Trim();
    }

    public async Task<string> GetBotbaseVersion(CancellationToken token)
    {
        // Allows up to 9 characters for version, and trims extra '\0' if unused.
        var bytes = await ReadRaw(SwitchCommand.GetBotbaseVersion(), 10, token).ConfigureAwait(false);
        return Encoding.ASCII.GetString(bytes).Trim('\0');
    }

    public async Task<string> GetGameInfo(string info, CancellationToken token)
    {
        var bytes = await ReadRaw(SwitchCommand.GetGameInfo(info), 17, token).ConfigureAwait(false);
        return Encoding.ASCII.GetString(bytes).Trim(['\0', '\n']);
    }

    public async Task<bool> IsProgramRunning(ulong pid, CancellationToken token)
    {
        var bytes = await ReadRaw(SwitchCommand.IsProgramRunning(pid), 17, token).ConfigureAwait(false);
        return ulong.TryParse(Encoding.ASCII.GetString(bytes).Trim(), out var value) && value == 1;
    }

    private async Task<byte[]> Read(ICommandBuilder b, ulong offset, int length, CancellationToken token)
    {
        if (length <= MaximumTransferSize)
        {
            var cmd = b.Peek(offset, length);
            return await ReadBytesFromCmdAsync(cmd, length, token).ConfigureAwait(false);
        }

        byte[] result = new byte[length];
        for (int i = 0; i < length; i += MaximumTransferSize)
        {
            int len = MaximumTransferSize;
            int delta = length - i;
            if (delta < MaximumTransferSize)
                len = delta;

            var cmd = b.Peek(offset + (uint)i, len);
            var bytes = await ReadBytesFromCmdAsync(cmd, len, token).ConfigureAwait(false);
            bytes.CopyTo(result, i);
            await Task.Delay((MaximumTransferSize / DelayFactor) + BaseDelay, token).ConfigureAwait(false);
        }
        return result;
    }

    private Task<byte[]> ReadMulti(ICommandBuilder b, IReadOnlyDictionary<ulong, int> offsetSizes, CancellationToken token)
    {
        var totalSize = offsetSizes.Values.Sum();
        var cmd = b.PeekMulti(offsetSizes);
        return ReadBytesFromCmdAsync(cmd, totalSize, token);
    }

    private async Task Write(ICommandBuilder b, byte[] data, ulong offset, CancellationToken token)
    {
        if (data.Length <= MaximumTransferSize)
        {
            var cmd = b.Poke(offset, data);
            await SendAsync(cmd, token).ConfigureAwait(false);
            return;
        }
        int byteCount = data.Length;
        for (int i = 0; i < byteCount; i += MaximumTransferSize)
        {
            var length = byteCount - i;
            if (length > MaximumTransferSize)
                length = MaximumTransferSize;
            var cmd = GetPoke(b, data, offset, i, length);
            await SendAsync(cmd, token).ConfigureAwait(false);
            await Task.Delay((MaximumTransferSize / DelayFactor) + BaseDelay, token).ConfigureAwait(false);
        }
    }

    private static byte[] GetPoke(ICommandBuilder b, byte[] data, ulong offset, int i, int length)
    {
        var slice = data.AsSpan(i, length);
        return b.Poke(offset + (uint)i, slice);
    }

    public async Task<byte[]> ReadRaw(byte[] command, int length, CancellationToken token)
    {
        await SendAsync(command, token).ConfigureAwait(false);
        var buffer = new byte[length];
        await Connection.ReceiveAsync(buffer, token);
        return buffer;
    }

    public async Task SendRaw(byte[] command, CancellationToken token)
    {
        await SendAsync(command, token).ConfigureAwait(false);
    }

    public Task<byte[]> PointerPeek(int size, IEnumerable<long> jumps, CancellationToken token)
    {
        return ReadBytesFromCmdAsync(SwitchCommand.PointerPeek(jumps, size), size, token);
    }

    public async Task PointerPoke(byte[] data, IEnumerable<long> jumps, CancellationToken token)
    {
        await SendAsync(SwitchCommand.PointerPoke(jumps, data), token).ConfigureAwait(false);
    }

    public async Task<ulong> PointerAll(IEnumerable<long> jumps, CancellationToken token)
    {
        var offsetBytes = await ReadBytesFromCmdAsync(SwitchCommand.PointerAll(jumps), sizeof(ulong), token).ConfigureAwait(false);
        Array.Reverse(offsetBytes, 0, 8);
        return BitConverter.ToUInt64(offsetBytes, 0);
    }

    public async Task<ulong> PointerRelative(IEnumerable<long> jumps, CancellationToken token)
    {
        var offsetBytes = await ReadBytesFromCmdAsync(SwitchCommand.PointerRelative(jumps), sizeof(ulong), token).ConfigureAwait(false);
        Array.Reverse(offsetBytes, 0, 8);
        return BitConverter.ToUInt64(offsetBytes, 0);
    }
}
