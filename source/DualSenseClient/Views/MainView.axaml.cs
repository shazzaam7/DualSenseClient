using System.Linq;
using Avalonia.Controls;
using DualSenseClient.Services;
using DualSenseClient.ViewModels;
using FluentAvalonia.UI.Controls;
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

        // Setup navigation service
        _navigationService.SetFrame(ContentFrame);
        _navigationService.SetNavigationView(NavView);

        DataContext = _viewModel;

#if DEBUG
        DebugItem.IsVisible = true;
        NavigationViewItem? debugItem = NavView.FooterMenuItems.OfType<NavigationViewItem>().FirstOrDefault(x => (string)x.Tag! == "Debug");
        NavView.SelectedItem = debugItem;
        _navigationService.NavigateToTag("Debug", ContentFrame);
#else
        NavigationViewItem? homeItem = NavView.MenuItems.OfType<NavigationViewItem>().FirstOrDefault(x => (string)x.Tag! == "Home");
        NavView.SelectedItem = homeItem;
        _navigationService.NavigateToTag("Home", ContentFrame);
#endif
    }

    // Functions
    private void NavView_OnItemInvoked(object? sender, NavigationViewItemInvokedEventArgs e)
    {
        if (e.InvokedItemContainer is NavigationViewItem selectedItem)
        {
            _navigationService.Navigate(selectedItem, ContentFrame);
        }
    }
}