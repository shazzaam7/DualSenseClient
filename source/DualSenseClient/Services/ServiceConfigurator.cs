using System;
using DualSenseClient.ViewModels;
using DualSenseClient.Views;
using Microsoft.Extensions.DependencyInjection;

namespace DualSenseClient.Services;

public static class ServiceConfigurator
{
    public static IServiceProvider ConfigureServices()
    {
        ServiceCollection services = new ServiceCollection();
        
        // Windows
        services.AddSingleton<MainWindow>();
        services.AddSingleton<MainWindowViewModel>();

        return services.BuildServiceProvider();
    }
}