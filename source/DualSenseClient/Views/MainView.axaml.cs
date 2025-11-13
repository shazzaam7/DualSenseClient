using System.Linq;
using Avalonia.Controls;
using DualSenseClient.Services;
using DualSenseClient.ViewModels;
using FluentAvalonia.UI.Controls;
using FluentIcons.Common;
using Microsoft.Extensions.DependencyInjection;

namespace DualSenseClient.Views;

public partial class MainView : UserControl
{
    // Properties
    private MainViewModel _viewModel { get; set; }
    private readonly NavigationService _navigationService;

    // Constructor
    public MainView()
    {
        InitializeComponent();
        _viewModel = App.Services.GetRequiredService<MainViewModel>();
        _navigationService = App.Services.GetRequiredService<NavigationService>();
        DataContext = _viewModel;

#if DEBUG
        DebugItem.IsVisible = true;
        NavView.SelectedItem = NavView.FooterMenuItems.OfType<NavigationViewItem>().FirstOrDefault(x => (string)x.Tag! == "Debug");
        _navigationService.NavigateToTag("Debug", ContentFrame);
        SetSelectedIcon(NavView.FooterMenuItems.OfType<NavigationViewItem>().FirstOrDefault(x => (string)x.Tag! == "Debug"));
#else
        NavView.SelectedItem = NavView.MenuItems.OfType<NavigationViewItem>().FirstOrDefault(x => (string)x.Tag! == "Home");
        _navigationService.NavigateToTag("Home", ContentFrame);
        SetSelectedIcon(NavView.MenuItems.OfType<NavigationViewItem>().FirstOrDefault(x => (string)x.Tag! == "Home"));
#endif
    }

    // Functions
    private void SetSelectedIcon(NavigationViewItem? selectedItem)
    {
        // Reset menu icons
        foreach (NavigationViewItem item in NavView.MenuItems.OfType<NavigationViewItem>())
        {
            if (item.Content is FluentIcons.Avalonia.Fluent.SymbolIcon icon)
            {
                icon.IconVariant = item == selectedItem ? IconVariant.Filled : IconVariant.Regular;
            }
        }

        // Reset footer icons
        foreach (NavigationViewItem item in NavView.FooterMenuItems.OfType<NavigationViewItem>())
        {
            if (item.Content is FluentIcons.Avalonia.Fluent.SymbolIcon icon)
            {
                icon.IconVariant = item == selectedItem ? IconVariant.Filled : IconVariant.Regular;
            }
        }
    }

    private void NavView_OnItemInvoked(object? sender, NavigationViewItemInvokedEventArgs e)
    {
        if (e.InvokedItemContainer is NavigationViewItem selectedItem)
        {
            _navigationService.Navigate(selectedItem, ContentFrame);
            SetSelectedIcon(selectedItem);
        }
    }
}