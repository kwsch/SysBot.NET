namespace SysBot.Base
{
    /// <summary>
    /// Bare minimum methods required to interact with a <see cref="IConsoleConnection"/> in a synchronous manner.
    /// </summary>
    public interface IConsoleConnectionSync : IConsoleConnection
    {
        int Send(byte[] buffer);

        byte[] ReadBytes(uint offset, int length);
        void WriteBytes(byte[] data, uint offset);
    }
}
