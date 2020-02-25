using System;
using System.Collections.Generic;

namespace SysBot.Base
{
    public static class LogUtil
    {
        //private static readonly ILogger Logger = LogManager.GetCurrentClassLogger();

        // hook in here if you want to forward the message elsewhere???
        public static readonly List<Action<string, string>> Forwarders = new List<Action<string, string>>();

        public static DateTime LastLogged { get; private set; } = DateTime.Now;

        public static void LogError(string message, string identity)
        {
            // Logger.Log(LogLevel.Error, level, message));
            Log(message, identity);
        }

        public static void LogInfo(string message, string identity)
        {
            // Logger.Log(LogLevel.Info, level, message));
            Log(message, identity);
        }

        private static void Log(string message, string identity)
        {
            Console.WriteLine(message);
            foreach (var fwd in Forwarders)
                fwd(message, identity);

            LastLogged = DateTime.Now;
        }
    }
}
