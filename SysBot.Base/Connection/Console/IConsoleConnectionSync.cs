namespace SysBot.Base
{
    public interface IConsoleConnectionSync : IConsoleConnection
    {
        int Send(byte[] buffer);

        byte[] ReadBytes(uint offset, int length);
        void WriteBytes(byte[] data, uint offset);
    }
}
