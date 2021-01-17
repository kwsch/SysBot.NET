namespace SysBot.Base
{
    /// <summary>
    /// Exposes the available interactions for synchronous communications with a Nintendo Switch.
    /// </summary>
    public interface ISwitchConnectionSync : IConsoleConnectionSync
    {
        ulong GetMainNsoBase();
        ulong GetHeapBase();

        byte[] ReadBytesMain(ulong offset, int length);
        byte[] ReadBytesAbsolute(ulong offset, int length);

        void WriteBytesMain(byte[] data, ulong offset);
        void WriteBytesAbsolute(byte[] data, ulong offset);
    }
}
