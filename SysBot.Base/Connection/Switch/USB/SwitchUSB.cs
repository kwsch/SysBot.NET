using LibUsbDotNet;
using LibUsbDotNet.Main;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;

namespace SysBot.Base;

/// <summary>
/// Abstract class representing the communication over USB.
/// </summary>
public abstract class SwitchUSB : IConsoleConnection
{
    public string Name { get; }
    public string Label { get; set; }
    public bool Connected { get; protected set; }
    private readonly int Port;

    protected SwitchUSB(int port)
    {
        Port = port;
        Name = Label = $"USB-{port}";
    }

    public void Log(string message) => LogInfo(message);
    public void LogInfo(string message) => LogUtil.LogInfo(message, Label);
    public void LogError(string message) => LogUtil.LogError(message, Label);

    private UsbDevice? SwDevice;
    private UsbEndpointReader? reader;
    private UsbEndpointWriter? writer;

    public int MaximumTransferSize { get; set; } = 0x1C0;
    public int BaseDelay { get; set; } = 1;
    public int DelayFactor { get; set; } = 1000;

    private readonly Lock _sync = new();
    private static readonly Lock _registry = new();

    public void Reset()
    {
        Disconnect();
        Connect();
    }

    public void Connect()
    {
        SwDevice = TryFindUSB();
        if (SwDevice == null)
            throw new Exception("USB device not found.");
        if (SwDevice is not IUsbDevice usb)
            throw new Exception("Device is using a WinUSB driver. Use libusbK and create a filter.");

        lock (_sync)
        {
            // UsbRegistryInfo is only supported on Windows.
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) && !usb.UsbRegistryInfo!.IsAlive)
                usb.ResetDevice();

            if (usb.IsOpen)
                usb.Close();
            usb.Open();

            usb.SetConfiguration(1);
            bool resagain = usb.ClaimInterface(0);
            if (!resagain)
            {
                usb.ReleaseInterface(0);
                usb.ClaimInterface(0);
            }

            reader = SwDevice.OpenEndpointReader(ReadEndpointID.Ep01);
            writer = SwDevice.OpenEndpointWriter(WriteEndpointID.Ep01);
        }
    }

    private UsbDevice? TryFindUSB()
    {
        lock (_registry)
        {
            foreach (var device in UsbDevice.AllLibUsbDevices)
            {
                if (device is not UsbRegistry ur)
                    continue;
                if (ur.Vid != 0x057E)
                    continue;
                if (ur.Pid != 0x3000)
                    continue;

                // Only Windows supports reading the port number from the registry.
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    ur.DeviceProperties.TryGetValue("Address", out var addr);
                    if (Port.ToString() != addr?.ToString())
                        continue;
                }

                return ur.Device;
            }
        }
        return null;
    }

    public void Disconnect()
    {
        lock (_sync)
        {
            if (SwDevice is { IsOpen: true } x)
            {
                if (x is IUsbDevice wholeUsbDevice)
                {
                    if (!wholeUsbDevice.UsbRegistryInfo.IsAlive)
                        wholeUsbDevice.ResetDevice();
                    wholeUsbDevice.ReleaseInterface(0);
                }
                x.Close();
            }

            reader?.Dispose();
            writer?.Dispose();
        }
    }

    public int Send(byte[] buffer)
    {
        lock (_sync)
            return SendInternal(buffer);
    }

    public int Read(byte[] buffer)
    {
        lock (_sync)
            return ReadInternal(buffer);
    }

    protected byte[] Read(ICommandBuilder b, ulong offset, int length)
    {
        var cmd = b.Peek(offset, length, false);
        SendInternal(cmd);
        return ReadBulkUSB();
    }

    protected byte[] ReadMulti(ICommandBuilder b, IReadOnlyDictionary<ulong, int> offsetSizes)
    {
        var cmd = b.PeekMulti(offsetSizes, false);
        SendInternal(cmd);
        return ReadBulkUSB();
    }

    protected byte[] ReadBulkUSB()
    {
        // Give it time to push back.
        Thread.Sleep(1);

        lock (_sync)
        {
            if (reader == null)
                throw new Exception("USB device not found or not connected.");

            // Let usb-botbase tell us the response size.
            byte[] sizeOfReturn = new byte[4];
            reader.Read(sizeOfReturn, 5000, out _);

            int size = BitConverter.ToInt32(sizeOfReturn, 0);
            byte[] buffer = new byte[size];

            // Loop until we have read everything.
            int transfSize = 0;
            while (transfSize < size)
            {
                Thread.Sleep(1);
                var ec = reader.Read(buffer, transfSize, Math.Min(reader.ReadBufferSize, size - transfSize), 5000, out int lenVal);
                if (ec != ErrorCode.None)
                {
                    Disconnect();
                    throw new Exception(UsbDevice.LastErrorString);
                }
                transfSize += lenVal;
            }
            return buffer;
        }
    }

    protected void Write(ICommandBuilder b, ReadOnlySpan<byte> data, ulong offset)
    {
        if (data.Length > MaximumTransferSize)
            WriteLarge(b, data, offset);
        else
            WriteSmall(b, data, offset);
    }

    public void WriteSmall(ICommandBuilder b, ReadOnlySpan<byte> data, ulong offset)
    {
        lock (_sync)
        {
            var cmd = b.Poke(offset, data, false);
            SendInternal(cmd);
            Thread.Sleep(1);
        }
    }

    private int ReadInternal(byte[] buffer)
    {
        byte[] sizeOfReturn = new byte[4];
        if (reader == null)
            throw new Exception("USB device not found or not connected.");

        reader.Read(sizeOfReturn, 5000, out _);
        reader.Read(buffer, 5000, out var lenVal);
        return lenVal;
    }

    private int SendInternal(byte[] buffer)
    {
        if (writer == null)
            throw new Exception("USB device not found or not connected.");

        uint pack = (uint)buffer.Length + 2;
        var ec = writer.Write(BitConverter.GetBytes(pack), 2000, out _);
        if (ec != ErrorCode.None)
        {
            Disconnect();
            throw new Exception(UsbDevice.LastErrorString);
        }
        ec = writer.Write(buffer, 2000, out var l);
        if (ec != ErrorCode.None)
        {
            Disconnect();
            throw new Exception(UsbDevice.LastErrorString);
        }
        return l;
    }

    private void WriteLarge(ICommandBuilder b, ReadOnlySpan<byte> data, ulong offset)
    {
        while (data.Length != 0)
        {
            var length = Math.Min(data.Length, MaximumTransferSize);
            var slice = data[..length];
            WriteSmall(b, slice, offset);

            data = data[length..];
            offset += (uint)length;
            Thread.Sleep((MaximumTransferSize / DelayFactor) + BaseDelay);
        }
    }
}
