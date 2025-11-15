using System;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core.Plugins;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using DualSenseClient.Core.DualSense;
using DualSenseClient.Core.Logging;
using DualSenseClient.Core.Settings;
using DualSenseClient.Services;
using DualSenseClient.Views;
using Microsoft.Extensions.DependencyInjection;
using NLog;
using Logger = DualSenseClient.Core.Logging.Logger;

namespace DualSenseClient;

public class App : Application
{
    // Properties
    public static Window? MainWindow => Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop ? desktop.MainWindow : null;
    public static IServiceProvider Services { get; private set; } = null!;

    // Functions
    public override void Initialize()
    {
        Logger.Debug<App>("Initializing Avalonia application");
        AvaloniaXamlLoader.Load(this);
        Logger.Debug<App>("Avalonia XAML loaded successfully");
    }

    public override void OnFrameworkInitializationCompleted()
    {
        Logger.Info<App>("Framework initialization started");

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            Logger.Debug<App>("Running as desktop application");

            // Disable Avalonia validation
            Logger.Trace<App>("Disabling Avalonia data annotation validation");
            DisableAvaloniaDataAnnotationValidation();

            // Configure services
            Logger.Debug<App>("Configuring dependency injection services");
            try
            {
                Services = ServiceConfigurator.ConfigureServices();
                Logger.Info<App>("Services configured successfully");
            }
            catch (Exception ex)
            {
                Logger.Error<App>("Failed to configure services");
                Logger.LogExceptionDetails<App>(ex);
                throw;
            }

            // Get MainWindow
            Logger.Debug<App>("Resolving MainWindow from services");
            MainWindow mainWindow = Services.GetRequiredService<MainWindow>();

            // Get settings and configure logger
            Logger.Debug<App>("Resolving settings manager");
            ISettingsManager settingsManager = Services.GetRequiredService<ISettingsManager>();

            string logLevel = settingsManager.Application.Debug.Logger.Level;
            Logger.Debug<App>($"Setting log level from settings: {logLevel}");
            Logger.SetLogLevel(LogLevelHelper.FromString(logLevel));

            // Initialize DualSense services
            Logger.Debug<App>("Initializing DualSense Manager");
            try
            {
                _ = Services.GetRequiredService<DualSenseManager>();
                Logger.Info<App>("DualSense Manager initialized");
            }
            catch (Exception ex)
            {
                Logger.Error<App>("Failed to initialize DualSense Manager");
                Logger.LogExceptionDetails<App>(ex, includeEnvironmentInfo: false);
            }

            Logger.Debug<App>("Initializing DualSense Profile Manager");
            try
            {
                _ = Services.GetRequiredService<DualSenseProfileManager>();
                Logger.Info<App>("DualSense Profile Manager initialized");
            }
            catch (Exception ex)
            {
                Logger.Error<App>("Failed to initialize DualSense Profile Manager");
                Logger.LogExceptionDetails<App>(ex, includeEnvironmentInfo: false);
            }

            // Wire up window events
            mainWindow.Opened += (_, _) =>
            {
                Logger.Info<App>("=== DualSense Client Started ===");
                Logger.Info<App>($"Version: {System.Reflection.Assembly.GetExecutingAssembly().GetName().Version}");
                Logger.Debug<App>("Main window opened");
            };

            mainWindow.Closing += (_, _) =>
            {
                Logger.Info<App>("Main window closing");
                Logger.Debug<App>("Flushing logs before shutdown");
                LogManager.Flush();
            };

            // Application exit handler
            desktop.Exit += (_, _) =>
            {
                Logger.Info<App>("=== DualSense Client Shutting Down ===");
                Logger.Debug<App>("Shutting down logger");
                Logger.Shutdown();
            };

            // Global exception handlers
            Logger.Trace<App>("Registering global exception handlers");

            TaskScheduler.UnobservedTaskException += (_, args) =>
            {
                args.SetObserved();
                Logger.Error<App>("Unobserved task exception occurred");
                HandleFatalException(args.Exception);
            };

            AppDomain.CurrentDomain.UnhandledException += (_, args) =>
            {
                bool isTerminating = args.IsTerminating;
                Logger.Error<App>($"Unhandled exception in AppDomain (Terminating: {isTerminating})");

                if (args.ExceptionObject is Exception ex)
                {
                    HandleFatalException(ex);
                }
                else
                {
                    Logger.Error<App>($"Non-exception object thrown: {args.ExceptionObject?.GetType().FullName ?? "null"}");
                }
            };

            Dispatcher.UIThread.UnhandledException += (_, args) =>
            {
                args.Handled = true;
                Logger.Error<App>("Unhandled exception on UI thread");
                HandleFatalException(args.Exception);
            };

            Logger.Debug<App>("Setting main window");
            desktop.MainWindow = mainWindow;

            Logger.Info<App>("Application initialization completed successfully");
        }
        else
        {
            Logger.Warning<App>("Application is not running as desktop application");
        }

        base.OnFrameworkInitializationCompleted();
    }

    private static void HandleFatalException(Exception ex)
    {
        try
        {
            Logger.Error<App>("=== Fatal Exception Encountered ===");
            Logger.LogExceptionDetails<App>(ex, includeEnvironmentInfo: true);

            // Ensure logs are written before potential crash
            LogManager.Flush();
        }
        catch
        {
            // If logging fails, we can't do much about it
            // Just ensure we don't throw from the exception handler
        }
    }

    private void DisableAvaloniaDataAnnotationValidation()
    {
        // Get an array of plugins to remove
        DataAnnotationsValidationPlugin[] dataValidationPluginsToRemove = BindingPlugins.DataValidators.OfType<DataAnnotationsValidationPlugin>().ToArray();

        int pluginCount = dataValidationPluginsToRemove.Length;
        Logger.Trace<App>($"Found {pluginCount} data annotation validation plugin(s) to remove");

        // remove each entry found
        foreach (DataAnnotationsValidationPlugin plugin in dataValidationPluginsToRemove)
        {
            BindingPlugins.DataValidators.Remove(plugin);
        }

        if (pluginCount > 0)
        {
            Logger.Debug<App>($"Removed {pluginCount} data annotation validation plugin(s)");
        }
    }
}