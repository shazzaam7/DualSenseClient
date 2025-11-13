using DualSenseClient.Core.Logging;
using DualSenseClient.Views.Pages;
using FluentAvalonia.UI.Controls;

namespace DualSenseClient.Services;

public class NavigationService
{
    public void Navigate(NavigationViewItem item, Frame contentFrame)
    {
        string tag = item.Tag?.ToString() ?? string.Empty;
        NavigateToTag(tag, contentFrame);
    }

    public void NavigateToTag(string tag, Frame contentFrame)
    {
        switch (tag)
        {
            case "Home":
                Logger.Info("Navigating to home page");
                contentFrame.Navigate(typeof(HomePage));
                break;
            case "Devices":
                Logger.Info("Navigating to devices page");
                contentFrame.Navigate(typeof(DevicesPage));
                break;
            case "Settings":
                Logger.Info("Navigating to settings page");
                contentFrame.Navigate(typeof(SettingsPage));
                break;
            case "Debug":
                Logger.Info("Navigating to debug page");
                contentFrame.Navigate(typeof(DebugPage));
                break;
            default:
                Logger.Warning($"Unknown tag: {tag}");
                break;
        }
    }
}