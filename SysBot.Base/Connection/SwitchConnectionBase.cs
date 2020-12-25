using System.Net.Sockets;

namespace SysBot.Base
{
    public abstract class SwitchConnectionBase
    {
        public Socket Connection = new(SocketType.Stream, ProtocolType.Tcp);
        public readonly string IP;
        public readonly int Port;

        public string Name { get; set; }
        public bool Connected { get; protected set; }

        public void Log(string message) => LogUtil.LogInfo(message, Name);

        protected SwitchConnectionBase(string ipaddress, int port)
        {
            IP = ipaddress;
            Port = port;
            Name = $"{IP}: {GetType().Name}";
            Log("Connection details created!");
        }
    }
}
