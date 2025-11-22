using CommunityToolkit.Mvvm.ComponentModel;
using DualSenseClient.Core.Settings;
using Microsoft.Extensions.DependencyInjection;

namespace DualSenseClient.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    [ObservableProperty] private string windowTitle = "DualSense Manager";

    public MainWindowViewModel()
    {
        WindowTitle = $"DualSense Manager v{App.Services.GetRequiredService<ISettingsManager>().Application.GetVersion()}";
    }
}