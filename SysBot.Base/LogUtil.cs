using System;
using System.Collections.Generic;
using NLog;

namespace SysBot.Base
{
    public static class LogUtil
    {
        private static readonly ILogger Logger = LogManager.GetCurrentClassLogger();

        // hook in here if you want to forward the message elsewhere???
        public static List<Action<string, string>> Forwarders = new List<Action<string, string>>();

        public static void Log(LogLevel level, string message, string identity)
        {
            Logger.Log(level, message);
            foreach (var fwd in Forwarders)
                fwd(message, identity);
        }
    }
}
