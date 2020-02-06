using System.Net.Sockets;
using NLog;

namespace SysBot.Base
{
    public abstract class SwitchConnectionBase
    {
        public readonly Socket Connection = new Socket(SocketType.Stream, ProtocolType.Tcp);
        public readonly string IP;
        public readonly int Port;

        public string Name { get; set; }
        public bool Connected { get; protected set; }

        public void Log(string message, LogLevel level) => LogUtil.Log(level, message, Name);
        public void Log(string message) => Log(message, LogLevel.Info);
        public void LogError(string message) => Log(message, LogLevel.Error);

        protected SwitchConnectionBase(string ipaddress, int port)
        {
            IP = ipaddress;
            Port = port;
            Name = $"{IP}: {GetType().Name}";
            Log("I'm Alive!");
        }
    }
}