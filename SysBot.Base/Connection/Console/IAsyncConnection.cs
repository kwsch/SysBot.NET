using System;

namespace SysBot.Base
{
    public interface IAsyncConnection
    {
        void ConnectCallback(IAsyncResult ar);
        void DisconnectCallback(IAsyncResult ar);
    }
}
