using System;
using System.Threading;
using System.Threading.Tasks;

namespace SysBot.Base
{
    public interface IRoutineExecutor
    {
        string LastLogged { get; }
        DateTime LastTime { get; }
        void ReportStatus();
        void Log(string message);
        string GetSummary();

        /// <summary>
        /// Connects to the console, then runs the bot.
        /// </summary>
        /// <param name="token">Cancel this token to have the bot stop looping.</param>
        Task RunAsync(CancellationToken token);

        Task MainLoop(CancellationToken token);
        Task InitialStartup(CancellationToken token);
        void SoftStop();
    }
}
