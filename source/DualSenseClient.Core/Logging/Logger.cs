using System.Text;
using DualSenseClient.Core.Paths;
using NLog;
using NLog.Config;
using NLog.Targets;

namespace DualSenseClient.Core.Logging;

public static class Logger
{
    // Properties
    private static readonly NLog.Logger _logger;
    private static readonly LoggingConfiguration _config;

    // Constructor
    static Logger()
    {
        _config = new LoggingConfiguration();

        // Console target (colored)
        ColoredConsoleTarget consoleTarget = new ColoredConsoleTarget("console")
        {
            Layout = @"[${longdate:format=HH\:mm\:ss.fff}][${level:uppercase=true:format=FirstCharacter}] ${message}"
        };
        consoleTarget.RowHighlightingRules.Add(new ConsoleRowHighlightingRule
        {
            Condition = "level == LogLevel.Warn",
            ForegroundColor = ConsoleOutputColor.Yellow
        });
        consoleTarget.RowHighlightingRules.Add(new ConsoleRowHighlightingRule
        {
            Condition = "level == LogLevel.Error",
            ForegroundColor = ConsoleOutputColor.Red
        });
        _config.AddTarget(consoleTarget);
        _config.AddRule(LogLevel.Trace, LogLevel.Fatal, consoleTarget);

#if !DEBUG
        // File target
        FileTarget fileTarget = new FileTarget("file")
        {
            FileName = PathResolver.LogFile,
            Layout = @"[${longdate:format=HH\:mm\:ss.fff}][${level:uppercase=true:format=FirstCharacter}] ${message}",
            KeepFileOpen = false,
            Encoding = Encoding.UTF8,
            ArchiveEvery = FileArchivePeriod.Day,
            MaxArchiveFiles = 7
        };
        _config.AddTarget(fileTarget);
        _config.AddRule(LogLevel.Trace, LogLevel.Fatal, fileTarget);
#endif

        LogManager.Configuration = _config;
        _logger = LogManager.GetCurrentClassLogger();
    }

    // Functions
    public static void SetLogLevel(LogLevel level)
    {
        IList<LoggingRule> rules = _config.LoggingRules;

        foreach (LoggingRule rule in rules)
        {
            rule.SetLoggingLevels(level, LogLevel.Fatal);
        }

        LogManager.ReconfigExistingLoggers();
        _logger.Info($"Logging level updated: {level}");
    }

    public static void Trace(string message) => _logger.Trace(message);
    public static void Debug(string message) => _logger.Debug(message);
    public static void Info(string message) => _logger.Info(message);
    public static void Warning(string message) => _logger.Warn(message);
    public static void Error(string message) => _logger.Error(message);

    public static void LogExceptionDetails(Exception ex, bool includeEnvironmentInfo = true)
    {
        Error("===== Exception Report Start =====");
        Error($"Timestamp (UTC): {DateTime.UtcNow:O}");

        LogExceptionWithDepth(ex);

        if (includeEnvironmentInfo)
        {
            Error("=== System Information ===");
            Error($"Machine Name: {Environment.MachineName}");
            Error($"OS Version: {Environment.OSVersion}");
            Error($".NET Runtime: {System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription}");
            Error($"Process Architecture: {System.Runtime.InteropServices.RuntimeInformation.ProcessArchitecture}");
            Error($"Current Directory: {Environment.CurrentDirectory}");
        }

        Error("===== Exception Report End =====");
    }

    private static void LogExceptionWithDepth(Exception ex, int depth = 0)
    {
        while (true)
        {
            string indent = new string(' ', depth * 2);
            Error($"{indent}Exception Level: {depth}");
            Error($"{indent}Type: {ex.GetType().FullName}");
            Error($"{indent}Message: {ex.Message}");
            Error($"{indent}Source: {ex.Source}");
            Error($"{indent}HResult: {ex.HResult}");
            if (ex.HelpLink != null)
            {
                Error($"{indent}Help Link: {ex.HelpLink}");
            }

            if (ex.Data.Count > 0)
            {
                Error($"{indent}Data:");
                foreach (object? key in ex.Data.Keys)
                {
                    Error($"{indent}  {key}: {ex.Data[key]}");
                }
            }

            if (!string.IsNullOrWhiteSpace(ex.StackTrace))
            {
                Error($"{indent}StackTrace:");
                foreach (string line in ex.StackTrace.Split(Environment.NewLine))
                {
                    Error($"{indent}  {line}");
                }
            }

            if (ex.TargetSite != null)
            {
                Error($"{indent}TargetSite: {ex.TargetSite}");
            }

            if (ex.InnerException != null)
            {
                Error($"{indent}--- Inner Exception ---");
                ex = ex.InnerException;
                depth = depth + 1;
                continue;
            }
            break;
        }
    }

    public static void Shutdown()
    {
        LogManager.Shutdown();
    }
}