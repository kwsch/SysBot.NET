using System;
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
        protected const int MaximumTransferSize = 468;

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
                    if (Port.ToString() != addr.ToString())
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
                if (SwDevice != null)
                {
                    Send(SwitchCommand.DetachController(false));
                    if (SwDevice.IsOpen)
                    {
                        if (SwDevice is IUsbDevice wholeUsbDevice)
                            wholeUsbDevice.ReleaseInterface(0);
                        SwDevice.Close();
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

        public byte[] Read(uint offset, int length)
        {
            var method = SwitchOffsetType.Heap.GetReadMethod(false);
            if (length > MaximumTransferSize)
                return ReadLarge(offset, length, method);
            return Read(offset, length, method);
        }

        public void Write(byte[] data, uint offset)
        {
            var method = SwitchOffsetType.Heap.GetWriteMethod(false);
            if (data.Length > MaximumTransferSize)
                WriteLarge(data, offset, method);
            Write(data, offset, method);
        }

        protected byte[] Read(ulong offset, int length, Func<ulong, int, byte[]> method)
        {
            lock (_sync)
            {
                var cmd = method(offset, length);
                SendInternal(cmd);
                Thread.Sleep(1);

                var buffer = new byte[length];
                var _ = ReadInternal(buffer);
                return buffer;
            }
        }

        protected void Write(byte[] data, ulong offset, Func<ulong, byte[], byte[]> method)
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

        private void WriteLarge(byte[] data, uint offset, Func<ulong, byte[], byte[]> method)
        {
            int byteCount = data.Length;
            for (int i = 0; i < byteCount; i += MaximumTransferSize)
                Write(data.Slice(i, MaximumTransferSize), offset + (uint)i, method);
        }

        private byte[] ReadLarge(uint offset, int length, Func<ulong, int, byte[]> method)
        {
            var result = new byte[length];
            for (int i = 0; i < length; i += MaximumTransferSize)
                Read(offset + (uint)i, Math.Min(MaximumTransferSize, length - i), method).CopyTo(result, i);
            return result;
        }

        protected byte[] ReadResponse(int length)
        {
            Thread.Sleep(1);
            lock (_sync)
            {
                var buffer = new byte[(length * 2) + 0];
                var _ = Read(buffer);
                return Decoder.ConvertHexByteStringToBytes(buffer);
            }
        }
    }
}
