using Serilog;
using Serilog.Core;
using Serilog.Events;
using System;
using System.Collections.Generic;
using System.IO;
using NetControlFlow.Config;

namespace NetControlFlow.Logging
{
    public static class LogManager
    {
        private static ILogger _logger;
        private static readonly object _lockObj = new object();

        public static void Initialize(LoggingConfig config)
        {
            lock (_lockObj)
            {
                if (!Directory.Exists(config.LogPath))
                    Directory.CreateDirectory(config.LogPath);

                var logFilePath = Path.Combine(
                    config.LogPath,
                    $"deobfuscator_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.log"
                );

                var logLevel = Enum.TryParse<LogEventLevel>(
                    config.LogLevel, 
                    ignoreCase: true, 
                    out var level) 
                    ? level 
                    : LogEventLevel.Information;

                _logger = new LoggerConfiguration()
                    .MinimumLevel.Is(logLevel)
                    .WriteTo.File(
                        logFilePath,
                        outputTemplate: config.LogFormat,
                        rollOnFileSizeLimit: true,
                        fileSizeLimitBytes: 10_000_000)
                    .WriteTo.Console(outputTemplate: config.LogFormat)
                    .CreateLogger();

                Serilog.Log.Logger = _logger;
            }
        }

        public static void LogOperation(string operation, Dictionary<string, object> variables = null)
        {
            var msg = $"[OPERATION] {operation}";
            if (variables != null && variables.Count > 0)
            {
                msg += Environment.NewLine + "Variables:";
                foreach (var kvp in variables)
                {
                    msg += Environment.NewLine + $"  {kvp.Key} = {kvp.Value}";
                }
            }
            _logger?.Information(msg);
        }

        public static void LogAnalysis(string item, string analysisType, Dictionary<string, object> data = null)
        {
            var msg = $"[ANALYSIS] {item} ({analysisType})";
            if (data != null && data.Count > 0)
            {
                msg += Environment.NewLine + "Data:";
                foreach (var kvp in data)
                {
                    msg += Environment.NewLine + $"  {kvp.Key} = {kvp.Value}";
                }
            }
            _logger?.Information(msg);
        }

        public static void LogError(string error, Exception ex = null, Dictionary<string, object> context = null)
        {
            var msg = $"[ERROR] {error}";
            if (context != null && context.Count > 0)
            {
                msg += Environment.NewLine + "Context:";
                foreach (var kvp in context)
                {
                    msg += Environment.NewLine + $"  {kvp.Key} = {kvp.Value}";
                }
            }
            _logger?.Error(ex, msg);
        }

        public static void LogSuccess(string message)
        {
            _logger?.Information($"[SUCCESS] {message}");
        }

        public static void LogDebug(string message, Dictionary<string, object> variables = null)
        {
            if (variables != null && variables.Count > 0)
            {
                var msg = $"{message} | Variables: {string.Join(", ", variables)}";
                _logger?.Debug(msg);
            }
            else
            {
                _logger?.Debug(message);
            }
        }
    }
}
