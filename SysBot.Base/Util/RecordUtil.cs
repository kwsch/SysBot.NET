using System.IO;
using System;

namespace SysBot.Base.Util
{
    public static class RecordUtil<T>
    {
        private static readonly string LogPath;

        static RecordUtil()
        {
            const string dir = "records";
            Directory.CreateDirectory(dir);
            LogPath = Path.Combine(dir, $"{typeof(T).Name}.txt");
        }

        public static void Record(string message)
        {
            try
            {
                File.AppendAllText(LogPath, $"{DateTime.Now:yyyy-MM-dd HH:mm:ss}\t{message}{Environment.NewLine}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to write to log: {ex.Message}");
            }
        }
    }
}
