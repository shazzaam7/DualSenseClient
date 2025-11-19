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
    public static IClassicDesktopStyleApplicationLifetime? Desktop = Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime;
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

        if (Desktop is IClassicDesktopStyleApplicationLifetime desktop)
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
            TrayIconService trayIconService = Services.GetRequiredService<TrayIconService>();

            // Initialize tray icon service
            trayIconService.Initialize();

            // Check if the application should start minimized
            bool shouldStartMinimized = Services.GetRequiredService<ISettingsManager>().Application.Ui.StartMinimized;

            bool isStartup = true; // Flag to track if it's the initial startup

            // Wire up window events
            mainWindow.Opened += (_, _) =>
            {
                Logger.Info<App>("=== DualSense Client Started ===");
                Logger.Info<App>($"Version: {System.Reflection.Assembly.GetExecutingAssembly().GetName().Version}");
                Logger.Debug<App>("Main window opened");

                // Only hide the window at initial startup if the setting is enabled
                if (shouldStartMinimized && isStartup)
                {
                    isStartup = false;
                    Logger.Info<App>("Start minimized setting is enabled, hiding window after startup");
                    trayIconService.HideMainWindow();
                }
            };

            mainWindow.Closing += (_, e) =>
            {
                Logger.Info<App>("Main window closing");

                // Check if we should close to tray instead of exiting the application
                if (trayIconService.ShouldCloseToTray())
                {
                    Logger.Info<App>("Minimizing to tray instead of closing");
                    e.Cancel = true; // Cancel the close event
                    trayIconService.HideMainWindow(); // Hide the main window
                }
                else
                {
                    Logger.Debug<App>("Flushing logs before shutdown");
                    LogManager.Flush();
                }
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
            Logger.Trace<App>($"Removed {pluginCount} data annotation validation plugin(s)");
        }
    }
}