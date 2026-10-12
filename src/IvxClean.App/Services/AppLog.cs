using System;
using System.IO;
using Serilog;

namespace IvxClean.App.Services;

/// <summary>Rolling file logging for the app. Never logs file contents — only operations and errors.</summary>
public static class AppLog
{
    private static ILogger? _logger;

    /// <summary>The shared logger (no-op until <see cref="Initialize"/> is called).</summary>
    public static ILogger Logger => _logger ?? Serilog.Core.Logger.None;

    /// <summary>The folder log files are written to.</summary>
    public static string LogDirectory { get; private set; } = string.Empty;

    /// <summary>Sets up rolling, size-capped file logging under the app data folder.</summary>
    public static void Initialize(string appDataRoot)
    {
        LogDirectory = Path.Combine(appDataRoot, "logs");
        Directory.CreateDirectory(LogDirectory);
        _logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .WriteTo.File(
                Path.Combine(LogDirectory, "ivxclean-.log"),
                rollingInterval: RollingInterval.Day,
                rollOnFileSizeLimit: true,
                fileSizeLimitBytes: 5 * 1024 * 1024,
                retainedFileCountLimit: 7,
                flushToDiskInterval: TimeSpan.FromSeconds(1),
                shared: true)
            .CreateLogger();
    }

    /// <summary>Logs an error with context.</summary>
    public static void Error(Exception ex, string context) => Logger.Error(ex, "{Context}", context);

    /// <summary>Logs an informational message.</summary>
    public static void Info(string message) => Logger.Information("{Message}", message);

    /// <summary>Flushes and closes the logger.</summary>
    public static void Shutdown() => (_logger as IDisposable)?.Dispose();
}
