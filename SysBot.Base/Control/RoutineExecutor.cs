using System;
using System.Threading;
using System.Threading.Tasks;

namespace SysBot.Base
{
    /// <summary>
    /// Commands a Bot to a perform a routine asynchronously.
    /// </summary>
    public abstract class RoutineExecutor<T> : IRoutineExecutor where T : class, IConsoleBotConfig
    {
        public readonly IConsoleConnectionAsync Connection;
        public readonly T Config;

        protected RoutineExecutor(IConsoleBotManaged<IConsoleConnection, IConsoleConnectionAsync> cfg)
        {
            Config = (T)cfg;
            Connection = cfg.CreateAsynchronous();
        }

        public string LastLogged { get; private set; } = "Not Started";
        public DateTime LastTime { get; private set; } = DateTime.Now;

        public void ReportStatus() => LastTime = DateTime.Now;

        public abstract string GetSummary();

        public void Log(string message)
        {
            Connection.Log(message);
            LastLogged = message;
            LastTime = DateTime.Now;
        }

        /// <summary>
        /// Connects to the console, then runs the bot.
        /// </summary>
        /// <param name="token">Cancel this token to have the bot stop looping.</param>
        public async Task RunAsync(CancellationToken token)
        {
            Connection.Connect();
            Log("Initializing connection with console...");
            await InitialStartup(token).ConfigureAwait(false);
            await MainLoop(token).ConfigureAwait(false);
            Connection.Disconnect();
        }

        public abstract Task MainLoop(CancellationToken token);
        public abstract Task InitialStartup(CancellationToken token);
        public abstract void SoftStop();
        public abstract Task HardStop();
    }
}
