using System.Linq;
using DualSenseClient.Core.Logging;
using DualSenseClient.Views.Pages;
using FluentAvalonia.UI.Controls;
using FluentIcons.Common;

namespace DualSenseClient.Services;

public class NavigationService
{
    private Frame? _contentFrame;
    private NavigationView? _navigationView;
    private string? _currentTag;

    public string? CurrentTag => _currentTag;

    public void SetFrame(Frame frame)
    {
        _contentFrame = frame;
    }

    public void SetNavigationView(NavigationView navigationView)
    {
        _navigationView = navigationView;
    }

    public void Navigate(NavigationViewItem item, Frame contentFrame)
    {
        string tag = item.Tag?.ToString() ?? string.Empty;
        NavigateToTag(tag, contentFrame);
        SetSelectedIcon(item);
    }

    public void NavigateToTag(string tag, Frame? contentFrame = null)
    {
        Frame? frame = contentFrame ?? _contentFrame;

        if (frame == null)
        {
            Logger.Warning("NavigationService: Cannot navigate: Frame is null");
            return;
        }

        _currentTag = tag;

        switch (tag)
        {
            case "Home":
                Logger.Info("NavigationService: Navigating to home page");
                frame.Navigate(typeof(HomePage));
                break;
            case "Monitor":
                Logger.Info("NavigationService: Navigating to monitor page");
                frame.Navigate(typeof(MonitorPage));
                break;
            case "Devices":
                Logger.Info("NavigationService: Navigating to devices page");
                frame.Navigate(typeof(DevicesPage));
                break;
            case "Settings":
                Logger.Info("NavigationService: Navigating to settings page");
                frame.Navigate(typeof(SettingsPage));
                break;
            case "Debug":
                Logger.Info("NavigationService: Navigating to debug page");
                frame.Navigate(typeof(DebugPage));
                break;
            default:
                Logger.Warning($"NavigationService: Unknown tag: {tag}");
                break;
        }

        // Update icon if navigating by tag
        if (_navigationView != null)
        {
            NavigationViewItem? item = FindNavigationItemByTag(tag);
            if (item != null)
            {
                SetSelectedIcon(item);
            }
        }

        UpdateSelection(tag);
    }

    private void UpdateSelection(string tag)
    {
        if (_navigationView == null)
        {
            return;
        }

        NavigationViewItem? item = FindNavigationItemByTag(tag);
        if (item != null)
        {
            _navigationView.SelectedItem = item;
            SetSelectedIcon(item);
        }
    }

    public void SetSelectedIcon(NavigationViewItem? selectedItem)
    {
        if (_navigationView == null)
        {
            Logger.Warning("NavigationService: Cannot set selected icon: NavigationView is null");
            return;
        }

        // Reset menu icons
        foreach (NavigationViewItem item in _navigationView.MenuItems.OfType<NavigationViewItem>())
        {
            if (item.Content is FluentIcons.Avalonia.Fluent.SymbolIcon icon)
            {
                icon.IconVariant = item == selectedItem ? IconVariant.Filled : IconVariant.Regular;
            }
        }

        // Reset footer icons
        foreach (NavigationViewItem item in _navigationView.FooterMenuItems.OfType<NavigationViewItem>())
        {
            if (item.Content is FluentIcons.Avalonia.Fluent.SymbolIcon icon)
            {
                icon.IconVariant = item == selectedItem ? IconVariant.Filled : IconVariant.Regular;
            }
        }
    }

    private NavigationViewItem? FindNavigationItemByTag(string tag)
    {
        if (_navigationView == null)
        {
            return null;
        }

        // Search in menu items
        NavigationViewItem? menuItem = _navigationView.MenuItems.OfType<NavigationViewItem>().FirstOrDefault(x => x.Tag?.ToString() == tag);

        if (menuItem != null)
        {
            return menuItem;
        }

        // Search in footer items
        return _navigationView.FooterMenuItems.OfType<NavigationViewItem>().FirstOrDefault(x => x.Tag?.ToString() == tag);
    }
}