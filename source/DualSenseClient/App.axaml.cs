using System;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core.Plugins;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
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
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            DisableAvaloniaDataAnnotationValidation();
            Services = ServiceConfigurator.ConfigureServices();
            MainWindow mainWindow = Services.GetRequiredService<MainWindow>();

            mainWindow.Opened += (_, _) =>
            {
                Logger.Info("DualSense Client started");
            };

            mainWindow.Closing += (_, _) =>
            {
                Logger.Info("Closing DualSense Client");
                LogManager.Flush();
            };

            desktop.Exit += (_, _) =>
            {
                Logger.Shutdown();
            };

            TaskScheduler.UnobservedTaskException += (_, args) =>
            {
                args.SetObserved();
                HandleFatalException(args.Exception);
            };

            AppDomain.CurrentDomain.UnhandledException += (_, args) =>
            {
                if (args.ExceptionObject is Exception ex)
                {
                    HandleFatalException(ex);
                }
            };

            Dispatcher.UIThread.UnhandledException += (_, args) =>
            {
                args.Handled = true;
                HandleFatalException(args.Exception);
            };

            desktop.MainWindow = mainWindow;
        }

        base.OnFrameworkInitializationCompleted();
    }

    private static void HandleFatalException(Exception ex)
    {
        Logger.Error("Exception encountered");
        Logger.LogExceptionDetails(ex);
    }

    private void DisableAvaloniaDataAnnotationValidation()
    {
        // Get an array of plugins to remove
        DataAnnotationsValidationPlugin[] dataValidationPluginsToRemove = BindingPlugins.DataValidators.OfType<DataAnnotationsValidationPlugin>().ToArray();

        // remove each entry found
        foreach (DataAnnotationsValidationPlugin plugin in dataValidationPluginsToRemove)
        {
            BindingPlugins.DataValidators.Remove(plugin);
        }
    }
}