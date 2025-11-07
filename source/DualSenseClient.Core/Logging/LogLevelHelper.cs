using NLog;

namespace DualSenseClient.Core.Logging;

public static class LogLevelHelper
{
    public static LogLevel FromString(string? levelName)
    {
        if (string.IsNullOrWhiteSpace(levelName))
        {
            return LogLevel.Debug;
        }

        return levelName.ToLowerInvariant() switch
        {
            "trace" => LogLevel.Trace,
            "debug" => LogLevel.Debug,
            "info" => LogLevel.Info,
            "warn" or "warning" => LogLevel.Warn,
            "error" => LogLevel.Error,
            "fatal" => LogLevel.Fatal,
            "off" => LogLevel.Off,
            _ => LogLevel.Info
        };
    }

    public static string ToString(LogLevel level)
    {
        return level.Name;
    }
}