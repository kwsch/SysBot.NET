using System;

namespace SysBot.Base
{
    /// <summary>
    /// Asynchronous notifications when calling <see cref="IConsoleConnection.Connect"/> and <see cref="IConsoleConnection.Disconnect"/>.
    /// </summary>
    public interface IAsyncConnection
    {
        void ConnectCallback(IAsyncResult ar);
        void DisconnectCallback(IAsyncResult ar);
    }
}
