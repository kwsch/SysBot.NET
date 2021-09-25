using System.IO;
using NLog;
using NLog.Config;
using NLog.Targets;

namespace SysBot.Base
{
    public static class RecordUtil<T>
    {
        private static LoggingConfiguration GetConfig()
        {
            var config = new LoggingConfiguration();
            const string dir = "records";
            Directory.CreateDirectory(dir);
            var name = typeof(T).Name;
            var record = new FileTarget("record")
            {
                FileName = Path.Combine(dir, $"{name}.txt"),
                ConcurrentWrites = true,

                ArchiveEvery = FileArchivePeriod.None,
                ArchiveNumbering = ArchiveNumberingMode.Sequence,
                ArchiveFileName = Path.Combine(dir, $"{name}.{{#}}.txt"),
                ArchiveAboveSize = 104857600, // 100MB (never)
                MaxArchiveFiles = 14,
            };
            config.AddRule(LogLevel.Debug, LogLevel.Fatal, record);
            return config;
        }

        // ReSharper disable once StaticMemberInGenericType
        private static readonly ILogger Logger = new LogFactory(GetConfig()).GetCurrentClassLogger();
        public static void Record(string message) => Logger.Log(LogLevel.Info, message);
    }
}
