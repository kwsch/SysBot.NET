using System;
using System.Collections.Generic;
using NLog;

namespace SysBot.Base
{
    public static class LogUtil
    {
        private static readonly ILogger Logger = new BotLogger();

        // hook in here if you want to forward the message elsewhere???
        public static List<Action<string>> Forwarders = new List<Action<string>>();

        public static void Log(LogLevel level, string message)
        {
            Logger.Log(level, message);
            foreach (var fwd in Forwarders)
                fwd(message);
        }
    }

    public class BotLogger : Logger
    {
    }
}
