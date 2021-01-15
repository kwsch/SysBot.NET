namespace SysBot.Base
{
    public abstract class SwitchUSB : IConsoleConnection
    {
        public string Name { get; }
        public string Label { get; set; }
        public bool Connected { get; protected set; }
        protected readonly int Port;

        protected SwitchUSB(int port)
        {
            Port = port;
            Name = Label = $"USB-{port}";
        }

        public void Log(string message) => LogInfo(message);
        public void LogInfo(string message) => LogUtil.LogInfo(message, Name);
        public void LogError(string message) => LogUtil.LogError(message, Name);

        public abstract void Connect();
        public abstract void Reset();
        public abstract void Disconnect();
    }
}
