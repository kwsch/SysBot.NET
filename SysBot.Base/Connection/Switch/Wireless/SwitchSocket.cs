using System.Net.Sockets;

namespace SysBot.Base
{
    public abstract class SwitchSocket : IConsoleConnection
    {
        protected Socket Connection;
        protected readonly IWirelessBotConfig Info;

        public string Name { get; }
        public string Label { get; set; }
        public bool Connected { get; protected set; }

        protected SwitchSocket(IWirelessBotConfig wi, SocketType type = SocketType.Stream, ProtocolType proto = ProtocolType.Tcp)
        {
            Connection = new Socket(type, proto);
            Info = wi;
            Name = Label = wi.IP;
        }

        public void Log(string message) => LogInfo(message);
        public void LogInfo(string message) => LogUtil.LogInfo(message, Name);
        public void LogError(string message) => LogUtil.LogError(message, Name);

        public abstract void Connect();
        public abstract void Reset();
        public abstract void Disconnect();
    }
}
