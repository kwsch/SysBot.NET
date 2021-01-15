namespace SysBot.Base
{
#pragma warning disable RCS1079 // Throwing of new NotImplementedException.
    public sealed class SwitchUSBSync : SwitchUSB, ISwitchConnectionSync
    {
        public SwitchUSBSync(int port) : base(port)
        {
        }

        public override void Connect()
        {
            throw new System.NotImplementedException();
        }

        public override void Reset()
        {
            throw new System.NotImplementedException();
        }

        public override void Disconnect()
        {
            throw new System.NotImplementedException();
        }

        public byte[] ReadBytes(uint offset, int length)
        {
            throw new System.NotImplementedException();
        }

        public void WriteBytes(byte[] data, uint offset)
        {
            throw new System.NotImplementedException();
        }

        public ulong GetMainNsoBase()
        {
            throw new System.NotImplementedException();
        }

        public ulong GetHeapBase()
        {
            throw new System.NotImplementedException();
        }

        public byte[] ReadBytesMain(ulong offset, int length)
        {
            throw new System.NotImplementedException();
        }

        public byte[] ReadBytesAbsolute(ulong offset, int length)
        {
            throw new System.NotImplementedException();
        }

        public void WriteBytesMain(byte[] data, ulong offset)
        {
            throw new System.NotImplementedException();
        }

        public void WriteBytesAbsolute(byte[] data, ulong offset)
        {
            throw new System.NotImplementedException();
        }
    }
}
