using System;
using System.Buffers;
using System.Threading;
using static SysBot.Base.SwitchOffsetTypeUtil;

namespace SysBot.Base;

/// <summary>
/// Connection to a Nintendo Switch hosting the sys-module via a socket (Wi-Fi).
/// </summary>
/// <remarks>
/// Interactions are performed synchronously.
/// </remarks>
public sealed class SwitchSocketSync(IWirelessConnectionConfig cfg) : SwitchSocket(cfg), ISwitchConnectionSync
{
    public override void Connect()
    {
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
        Log("Disconnected!");
        InitializeSocket();
    }

    private int Read(byte[] buffer, int size) => Connection.Receive(buffer, size, 0);
    public int Send(byte[] buffer) => Connection.Send(buffer);

    private byte[] ReadResponse(int length)
    {
        // give it time to push data back
        Thread.Sleep((MaximumTransferSize / DelayFactor) + BaseDelay);
        var size = (length * 2) + 1;
        var buffer = ArrayPool<byte>.Shared.Rent(size);
        _ = Read(buffer, size);
        var mem = buffer.AsMemory(0, size);
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

    public ulong GetMainNsoBase()
    {
        Send(SwitchCommand.GetMainNsoBase());
        byte[] baseBytes = ReadResponse(8);
        Array.Reverse(baseBytes, 0, 8);
        return BitConverter.ToUInt64(baseBytes, 0);
    }

    public ulong GetHeapBase()
    {
        Send(SwitchCommand.GetHeapBase());
        byte[] baseBytes = ReadResponse(8);
        Array.Reverse(baseBytes, 0, 8);
        return BitConverter.ToUInt64(baseBytes, 0);
    }

    public byte[] ReadBytes(uint offset, int length) => Read(Heap, offset, length);
    public byte[] ReadBytesMain(ulong offset, int length) => Read(Main, offset, length);
    public byte[] ReadBytesAbsolute(ulong offset, int length) => Read(Absolute, offset, length);

    public void WriteBytes(ReadOnlySpan<byte> data, uint offset) => Write(Heap, data, offset);
    public void WriteBytesMain(ReadOnlySpan<byte> data, ulong offset) => Write(Main, data, offset);
    public void WriteBytesAbsolute(ReadOnlySpan<byte> data, ulong offset) => Write(Absolute, data, offset);

    private byte[] Read(ICommandBuilder b, ulong offset, int length)
    {
        if (length <= MaximumTransferSize)
        {
            var cmd = b.Peek(offset, length);
            Send(cmd);
            return ReadResponse(length);
        }

        byte[] result = new byte[length];
        for (int i = 0; i < length; i += MaximumTransferSize)
        {
            int len = MaximumTransferSize;
            int delta = length - i;
            if (delta < MaximumTransferSize)
                len = delta;

            var cmd = b.Peek(offset + (uint)i, len);
            Send(cmd);
            var bytes = ReadResponse(len);
            bytes.CopyTo(result, i);
        }
        return result;
    }

    private void Write(ICommandBuilder b, ReadOnlySpan<byte> data, ulong offset)
    {
        if (data.Length <= MaximumTransferSize)
        {
            var cmd = b.Poke(offset, data);
            Send(cmd);
            return;
        }
        while (data.Length != 0)
        {
            var length = Math.Min(data.Length, MaximumTransferSize);
            var slice = data[..length];
            var cmd = b.Poke(offset, slice);
            Send(cmd);

            data = data[length..];
            offset += (uint)length;
            Thread.Sleep((MaximumTransferSize / DelayFactor) + BaseDelay);
        }
    }
}
