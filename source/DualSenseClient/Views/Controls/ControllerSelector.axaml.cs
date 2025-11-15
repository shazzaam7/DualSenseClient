using Avalonia.Controls;
using Avalonia.Input;
using DualSenseClient.ViewModels.Controls;
using Microsoft.Extensions.DependencyInjection;

namespace DualSenseClient.Views.Controls;

public partial class ControllerSelector : UserControl
{
    private ControllerSelectorViewModel _viewModel { get; set; }

    public ControllerSelector()
    {
        InitializeComponent();
        _viewModel = App.Services.GetRequiredService<ControllerSelectorViewModel>();
        DataContext = _viewModel;
    }

    public ControllerSelectorViewModel ViewModel => _viewModel;

    private void OnRenameTextBoxKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            if (sender is TextBox { DataContext: ControllerListItemViewModel viewModel })
            {
                viewModel.SaveNameCommand.Execute(null);
                e.Handled = true;
            }
        }
        else if (e.Key == Key.Escape)
        {
            if (sender is TextBox { DataContext: ControllerListItemViewModel viewModel })
            {
                viewModel.CancelRenamingCommand.Execute(null);
                e.Handled = true;
            }
        }
    }
}