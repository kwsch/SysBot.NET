using System;
using System.Collections.Generic;
using NLog;
using NLog.Config;
using NLog.Targets;

namespace SysBot.Base
{
    public static class LogUtil
    {
        static LogUtil()
        {
            var config = new LoggingConfiguration();
            var logfile = new FileTarget("logfile")
            {
                FileName = "SysBotLog.txt",
                ConcurrentWrites = true,

                ArchiveEvery = FileArchivePeriod.Day,
                ArchiveNumbering = ArchiveNumberingMode.Date,
                ArchiveFileName = "SysBotLog.{#}.txt",
                ArchiveDateFormat = "yyyy-MM-dd",
                ArchiveAboveSize = 104857600, // 100MB (never)
                MaxArchiveFiles = 14, // 2 weeks
            };
            config.AddRule(LogLevel.Debug, LogLevel.Fatal, logfile);
            LogManager.Configuration = config;
        }

        private static readonly ILogger Logger = LogManager.GetCurrentClassLogger();

        // hook in here if you want to forward the message elsewhere???
        public static readonly List<Action<string, string>> Forwarders = new List<Action<string, string>>();

        public static DateTime LastLogged { get; private set; } = DateTime.Now;

        public static void LogError(string message, string identity)
        {
            Logger.Log(LogLevel.Error, message);
            Log(message, identity);
        }

        public static void LogInfo(string message, string identity)
        {
            Logger.Log(LogLevel.Info, message);
            Log(message, identity);
        }

        private static void Log(string message, string identity)
        {
            foreach (var fwd in Forwarders)
                fwd(message, identity);

            LastLogged = DateTime.Now;
        }
    }
}
