using System;
using System.Diagnostics;
using System.IO;

namespace Pie.Services
{
    public enum LogLevel
    {
        Debug,
        Info,
        Warning,
        Error
    }

    public static class LogService
    {
        private static readonly string LogDirectory;
        private static readonly string LogFilePath;
        private static readonly object LockObj = new();
        private static bool _isDebugMode;

        static LogService()
        {
            LogDirectory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Pie",
                "Logs"
            );
            Directory.CreateDirectory(LogDirectory);

            var timestamp = DateTime.Now.ToString("yyyy-MM-dd");
            LogFilePath = Path.Combine(LogDirectory, $"pie-{timestamp}.log");

            // Check if debug mode is enabled
            _isDebugMode = Environment.GetEnvironmentVariable("PIE_DEBUG") == "true"
                || Debugger.IsAttached;

#if DEBUG
            _isDebugMode = true;
#endif
        }

        public static void Debug(string message)
        {
            if (_isDebugMode)
            {
                Log(LogLevel.Debug, message);
            }
        }

        public static void Info(string message)
        {
            Log(LogLevel.Info, message);
        }

        public static void Warning(string message)
        {
            Log(LogLevel.Warning, message);
        }

        public static void Error(string message, Exception? exception = null)
        {
            var fullMessage = exception != null
                ? $"{message}\nException: {exception.GetType().Name}: {exception.Message}\nStack Trace: {exception.StackTrace}"
                : message;
            Log(LogLevel.Error, fullMessage);
        }

        public static event Action<string>? LogMessage;

        private static void Log(LogLevel level, string message)
        {
            var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
            var logEntry = $"[{timestamp}] [{level.ToString().ToUpper()}] {message}";

            // Write to console (visible when running from command line)
            var originalColor = Console.ForegroundColor;
            Console.ForegroundColor = level switch
            {
                LogLevel.Debug => ConsoleColor.Gray,
                LogLevel.Info => ConsoleColor.White,
                LogLevel.Warning => ConsoleColor.Yellow,
                LogLevel.Error => ConsoleColor.Red,
                _ => ConsoleColor.White
            };
            Console.WriteLine(logEntry);
            Console.ForegroundColor = originalColor;

            // Write to debug output (visible in Visual Studio)
            System.Diagnostics.Debug.WriteLine(logEntry);

            // Invoke event
            LogMessage?.Invoke(logEntry);

            // Write to log file
            try
            {
                lock (LockObj)
                {
                    File.AppendAllText(LogFilePath, logEntry + Environment.NewLine);
                }
            }
            catch
            {
                // Ignore file write errors
            }
        }

        public static string GetLogFilePath() => LogFilePath;

        public static string GetLogDirectory() => LogDirectory;

        public static void CleanOldLogs(int keepDays = 7)
        {
            try
            {
                var cutoffDate = DateTime.Now.AddDays(-keepDays);
                foreach (var file in Directory.GetFiles(LogDirectory, "pie-*.log"))
                {
                    var fileInfo = new FileInfo(file);
                    if (fileInfo.CreationTime < cutoffDate)
                    {
                        fileInfo.Delete();
                    }
                }
            }
            catch
            {
                // Ignore cleanup errors
            }
        }
    }
}
