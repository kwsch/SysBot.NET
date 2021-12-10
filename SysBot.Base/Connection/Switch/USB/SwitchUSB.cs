﻿using System;
using System.Collections.Generic;
using System.Threading;
using LibUsbDotNet;
using LibUsbDotNet.Main;

namespace SysBot.Base
{
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

        private readonly object _sync = new();
        private static readonly object _registry = new();

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
                if (!usb.UsbRegistryInfo.IsAlive)
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
                foreach (UsbRegistry ur in UsbDevice.AllLibUsbDevices)
                {
                    if (ur.Vid != 0x057E)
                        continue;
                    if (ur.Pid != 0x3000)
                        continue;

                    ur.DeviceProperties.TryGetValue("Address", out object addr);
                    if (Port.ToString() != addr?.ToString())
                        continue;

                    return ur.Device;
                }
            }
            return null;
        }

        public void Disconnect()
        {
            lock (_sync)
            {
                if (SwDevice is { } x)
                {
                    if (x.IsOpen)
                    {
                        if (x is IUsbDevice wholeUsbDevice)
                        {
                            if (!wholeUsbDevice.UsbRegistryInfo.IsAlive)
                                wholeUsbDevice.ResetDevice();
                            wholeUsbDevice.ReleaseInterface(0);
                        }
                        x.Close();
                    }
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

        protected byte[] Read(ulong offset, int length, Func<ulong, int, byte[]> method)
        {
            var cmd = method(offset, length);
            SendInternal(cmd);
            return ReadBulkUSB();
        }

        protected byte[] ReadMulti(IReadOnlyDictionary<ulong, int> offsetSizes, Func<IReadOnlyDictionary<ulong, int>, byte[]> method)
        {
            var cmd = method(offsetSizes);
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

        protected void Write(byte[] data, ulong offset, Func<ulong, byte[], byte[]> method)
        {
            if (data.Length > MaximumTransferSize)
                WriteLarge(data, offset, method);
            else WriteSmall(data, offset, method);
        }

        public void WriteSmall(byte[] data, ulong offset, Func<ulong, byte[], byte[]> method)
        {
            lock (_sync)
            {
                var cmd = method(offset, data);
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

        private void WriteLarge(byte[] data, ulong offset, Func<ulong, byte[], byte[]> method)
        {
            int byteCount = data.Length;
            for (int i = 0; i < byteCount; i += MaximumTransferSize)
            {
                var slice = data.SliceSafe(i, MaximumTransferSize);
                Write(slice, offset + (uint)i, method);
                Thread.Sleep((MaximumTransferSize / DelayFactor) + BaseDelay);
            }
        }
    }
}