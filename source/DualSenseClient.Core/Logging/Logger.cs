using System.Runtime.CompilerServices;
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
        consoleTarget.RowHighlightingRules.Add(new ConsoleRowHighlightingRule
        {
            Condition = "level == LogLevel.Fatal",
            ForegroundColor = ConsoleOutputColor.DarkRed
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

    /// <summary>
    /// Gets a clean type name, handling generics and nested classes
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static string GetTypeName<T>()
    {
        Type type = typeof(T);

        // Handle generic types - remove `1, `2, etc.
        string typeName = type.Name;
        int backtickIndex = typeName.IndexOf('`');
        if (backtickIndex > 0)
        {
            typeName = typeName.Substring(0, backtickIndex);
        }

        // Handle nested types - include parent class
        if (type is not { IsNested: true, DeclaringType: not null })
        {
            return typeName;
        }
        string declaringName = type.DeclaringType.Name;
        int declaringBacktick = declaringName.IndexOf('`');
        if (declaringBacktick > 0)
        {
            declaringName = declaringName.Substring(0, declaringBacktick);
        }
        return $"{declaringName}.{typeName}";
    }

    // Type-based logging (class name only)
    public static void Trace<T>(string message)
    {
        _logger.Trace($"[{GetTypeName<T>()}] {message}");
    }

    public static void Debug<T>(string message)
    {
        _logger.Debug($"[{GetTypeName<T>()}] {message}");
    }

    public static void Info<T>(string message)
    {
        _logger.Info($"[{GetTypeName<T>()}] {message}");
    }

    public static void Warning<T>(string message)
    {
        _logger.Warn($"[{GetTypeName<T>()}] {message}");
    }

    public static void Error<T>(string message)
    {
        _logger.Error($"[{GetTypeName<T>()}] {message}");
    }

    public static void Fatal<T>(string message)
    {
        _logger.Fatal($"[{GetTypeName<T>()}] {message}");
    }

    // Type-based logging WITH method names
    public static void Trace<T>(string message, [CallerMemberName] string? methodName = null)
    {
        string className = GetTypeName<T>();
        string prefix = string.IsNullOrEmpty(methodName) ? className : $"{className}.{methodName}";
        _logger.Trace($"[{prefix}] {message}");
    }

    public static void Debug<T>(string message, [CallerMemberName] string? methodName = null)
    {
        string className = GetTypeName<T>();
        string prefix = string.IsNullOrEmpty(methodName) ? className : $"{className}.{methodName}";
        _logger.Debug($"[{prefix}] {message}");
    }

    public static void Info<T>(string message, [CallerMemberName] string? methodName = null)
    {
        string className = GetTypeName<T>();
        string prefix = string.IsNullOrEmpty(methodName) ? className : $"{className}.{methodName}";
        _logger.Info($"[{prefix}] {message}");
    }

    public static void Warning<T>(string message, [CallerMemberName] string? methodName = null)
    {
        string className = GetTypeName<T>();
        string prefix = string.IsNullOrEmpty(methodName) ? className : $"{className}.{methodName}";
        _logger.Warn($"[{prefix}] {message}");
    }

    public static void Error<T>(string message, [CallerMemberName] string? methodName = null)
    {
        string className = GetTypeName<T>();
        string prefix = string.IsNullOrEmpty(methodName) ? className : $"{className}.{methodName}";
        _logger.Error($"[{prefix}] {message}");
    }

    public static void Fatal<T>(string message, [CallerMemberName] string? methodName = null)
    {
        string className = GetTypeName<T>();
        string prefix = string.IsNullOrEmpty(methodName) ? className : $"{className}.{methodName}";
        _logger.Fatal($"[{prefix}] {message}");
    }

    public static void LogExceptionDetails<T>(Exception ex, bool includeEnvironmentInfo = true)
    {
        string className = GetTypeName<T>();
        _logger.Error($"[{className}] ===== Exception Report Start =====");
        _logger.Error($"[{className}] Timestamp (UTC): {DateTime.UtcNow:O}");

        LogExceptionWithDepth(ex, className);

        if (includeEnvironmentInfo)
        {
            _logger.Error($"[{className}] === System Information ===");
            _logger.Error($"[{className}] Machine Name: {Environment.MachineName}");
            _logger.Error($"[{className}] OS Version: {Environment.OSVersion}");
            _logger.Error($"[{className}] .NET Runtime: {System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription}");
            _logger.Error($"[{className}] Process Architecture: {System.Runtime.InteropServices.RuntimeInformation.ProcessArchitecture}");
            _logger.Error($"[{className}] Current Directory: {Environment.CurrentDirectory}");
        }

        _logger.Error($"[{className}] ===== Exception Report End =====");
    }

    // Original logging (for backward compatibility - DEPRECATED)
    [Obsolete("Use Logger.Trace<T>(message) instead for class-specific logging")]
    public static void Trace(string message) => _logger.Trace(message);

    [Obsolete("Use Logger.Debug<T>(message) instead for class-specific logging")]
    public static void Debug(string message) => _logger.Debug(message);

    [Obsolete("Use Logger.Info<T>(message) instead for class-specific logging")]
    public static void Info(string message) => _logger.Info(message);

    [Obsolete("Use Logger.Warning<T>(message) instead for class-specific logging")]
    public static void Warning(string message) => _logger.Warn(message);

    [Obsolete("Use Logger.Error<T>(message) instead for class-specific logging")]
    public static void Error(string message) => _logger.Error(message);

    [Obsolete("Use Logger.Fatal<T>(message) instead for class-specific logging")]
    public static void Fatal(string message) => _logger.Fatal(message);

    [Obsolete("Use Logger.LogExceptionDetails<T>(ex) instead for class-specific logging")]
    public static void LogExceptionDetails(Exception ex, bool includeEnvironmentInfo = true)
    {
        _logger.Error("===== Exception Report Start =====");
        _logger.Error($"Timestamp (UTC): {DateTime.UtcNow:O}");

        LogExceptionWithDepth(ex);

        if (includeEnvironmentInfo)
        {
            _logger.Error("=== System Information ===");
            _logger.Error($"Machine Name: {Environment.MachineName}");
            _logger.Error($"OS Version: {Environment.OSVersion}");
            _logger.Error($".NET Runtime: {System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription}");
            _logger.Error($"Process Architecture: {System.Runtime.InteropServices.RuntimeInformation.ProcessArchitecture}");
            _logger.Error($"Current Directory: {Environment.CurrentDirectory}");
        }

        _logger.Error("===== Exception Report End =====");
    }

    private static void LogExceptionWithDepth(Exception ex, string? className = null, int depth = 0)
    {
        while (true)
        {
            string indent = new string(' ', depth * 2);
            string prefix = className != null ? $"[{className}] " : "";

            _logger.Error($"{prefix}{indent}Exception Level: {depth}");
            _logger.Error($"{prefix}{indent}Type: {ex.GetType().FullName}");
            _logger.Error($"{prefix}{indent}Message: {ex.Message}");
            _logger.Error($"{prefix}{indent}Source: {ex.Source}");
            _logger.Error($"{prefix}{indent}HResult: {ex.HResult}");
            if (ex.HelpLink != null)
            {
                _logger.Error($"{prefix}{indent}Help Link: {ex.HelpLink}");
            }

            if (ex.Data.Count > 0)
            {
                _logger.Error($"{prefix}{indent}Data:");
                foreach (object? key in ex.Data.Keys)
                {
                    _logger.Error($"{prefix}{indent}  {key}: {ex.Data[key]}");
                }
            }

            if (!string.IsNullOrWhiteSpace(ex.StackTrace))
            {
                _logger.Error($"{prefix}{indent}StackTrace:");
                foreach (string line in ex.StackTrace.Split(Environment.NewLine))
                {
                    _logger.Error($"{prefix}{indent}  {line}");
                }
            }

            if (ex.TargetSite != null)
            {
                _logger.Error($"{prefix}{indent}TargetSite: {ex.TargetSite}");
            }

            if (ex.InnerException != null)
            {
                _logger.Error($"{prefix}{indent}--- Inner Exception ---");
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