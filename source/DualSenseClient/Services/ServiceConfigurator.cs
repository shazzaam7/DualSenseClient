using System;
using DualSenseClient.Core.DualSense;
using DualSenseClient.ViewModels;
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
        services.AddSingleton<DualSenseManager>();
        services.AddSingleton<NavigationService>();

        // ViewModels
        services.AddSingleton<MainWindowViewModel>();
        services.AddSingleton<MainViewModel>();
        services.AddSingleton<HomePageViewModel>();
        services.AddSingleton<SettingsPageViewModel>();
        services.AddSingleton<DebugPageViewModel>();

        // Windows
        services.AddSingleton<MainWindow>();

        return services.BuildServiceProvider();
    }
}