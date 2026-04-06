using System;
using Serilog;

namespace NetControlFlow.Logging
{
    public class LoggingConfig
    {
        public string LogPath { get; set; } = "./logs/deobfuscator.log";
        public string LogLevel { get; set; } = "Information";
        public bool ConsoleOutput { get; set; } = true;
    }

    public static class LogManager
    {
        private static bool _initialized = false;

        public static void Initialize(LoggingConfig config)
        {
            if (_initialized) return;

            var logConfig = new LoggerConfiguration()
                .MinimumLevel.Information()
                .WriteTo.File(config.LogPath, rollingInterval: RollingInterval.Day);

            if (config.ConsoleOutput)
            {
                logConfig.WriteTo.Console();
            }

            Log.Logger = logConfig.CreateLogger();
            _initialized = true;
        }

        public static void LogInfo(string message) => Log.Information(message);
        public static void LogWarning(string message) => Log.Warning(message);
        public static void LogError(string message, Exception? ex = null) => Log.Error(ex, message);
        public static void LogDebug(string message) => Log.Debug(message);
    }
}
