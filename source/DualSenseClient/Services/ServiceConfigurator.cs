using System;
using DualSenseClient.Core.DualSense;
using DualSenseClient.Core.Settings;
using DualSenseClient.ViewModels;
using DualSenseClient.ViewModels.Controls;
using DualSenseClient.ViewModels.Pages;
using DualSenseClient.Views;
using Microsoft.Extensions.DependencyInjection;

namespace DualSenseClient.Services;

public static class ServiceConfigurator
{
    public static IServiceProvider ConfigureServices()
    {
        ServiceCollection services = new ServiceCollection();

        // Core
        services.AddSingleton<SelectedControllerService>();
        services.AddSingleton<DualSenseManager>();
        services.AddSingleton<DualSenseProfileManager>();
        services.AddSingleton<NavigationService>();
        services.AddSingleton<IApplicationSettings, ApplicationSettings>();
        services.AddSingleton<ISettingsManager, SettingsManager>();

        // ViewModels
        services.AddSingleton<ControllerSelectorViewModel>();
        services.AddSingleton<MainWindowViewModel>();
        services.AddSingleton<MainViewModel>();
        services.AddSingleton<HomePageViewModel>();
        services.AddSingleton<SettingsPageViewModel>();
        services.AddSingleton<DevicesPageViewModel>();
        services.AddSingleton<DebugPageViewModel>();

        // Windows
        services.AddSingleton<MainWindow>();

        return services.BuildServiceProvider();
    }
}