using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform;
using CommunityToolkit.Mvvm.Input;
using DualSenseClient.Core.Logging;
using DualSenseClient.Core.Settings;
using DualSenseClient.ViewModels;

namespace DualSenseClient.Services;

public class TrayIconService : IDisposable
{
    private TrayIcon? _trayIcon;
    private readonly SelectedControllerService _selectedControllerService;
    private readonly ISettingsManager _settingsManager;
    private readonly List<ControllerViewModelBase> _controllers = new();
    private readonly TrayIconViewModel _viewModel;
    private bool _disposed = false;

    public TrayIconService(SelectedControllerService selectedControllerService, ISettingsManager settingsManager)
    {
        _selectedControllerService = selectedControllerService;
        _settingsManager = settingsManager;
        _viewModel = new TrayIconViewModel(ShowMainWindow);

        // Subscribe to available controllers collection changes
        _selectedControllerService.AvailableControllers.CollectionChanged += (_, _) =>
        {
            UpdateControllers();
        };

        // Subscribe to controller changes (battery percentage going down/up, rename...)
        _selectedControllerService.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(SelectedControllerService.SelectedController))
            {
                UpdateTrayIcon();
            }
        };
    }

    public void Initialize()
    {
        Logger.Info<TrayIconService>("Initializing tray icon service");

        try
        {
            _trayIcon = new TrayIcon();

            // Set the default icon
            string iconPath = "avares://DualSenseClient/Assets/icon.ico";
            _trayIcon.Icon = new WindowIcon(AssetLoader.Open(new Uri(iconPath)));

            // Update controllers list and set initial icon
            UpdateControllers();

            // Create context menu
            _trayIcon.Menu = CreateContextMenu();

            // TODO: Add direct left-click event or double-click to show the app

            // Show the tray icon
            _trayIcon.IsVisible = true;

            Logger.Info<TrayIconService>("Tray icon service initialized successfully");
        }
        catch (Exception ex)
        {
            Logger.Error<TrayIconService>($"Failed to initialize tray icon: {ex.Message}");
            Logger.LogExceptionDetails<TrayIconService>(ex);
        }
    }

    private NativeMenu CreateContextMenu()
    {
        NativeMenu nativeMenu = new NativeMenu();

        // Show main window item
        NativeMenuItem showItem = new NativeMenuItem("Show")
        {
            Command = _viewModel.ShowMainWindowCommand
        };
        nativeMenu.Items.Add(showItem);

        // Add controller items if there are any controllers
        if (_controllers.Any())
        {
            nativeMenu.Items.Add(new NativeMenuItemSeparator());

            foreach (ControllerViewModelBase controller in _controllers)
            {
                NativeMenuItem controllerItem = new NativeMenuItem($"{controller.Name} ({controller.BatteryText})")
                {
                    Command = new RelayCommand(() => SelectController(controller))
                };
                nativeMenu.Items.Add(controllerItem);
            }
        }

        // Separator before exit
        nativeMenu.Items.Add(new NativeMenuItemSeparator());

        // Exit item
        NativeMenuItem exitItem = new NativeMenuItem("Exit")
        {
            Command = _viewModel.ExitApplicationCommand
        };
        nativeMenu.Items.Add(exitItem);

        return nativeMenu;
    }

    private void SelectController(ControllerViewModelBase controller)
    {
        _selectedControllerService.SelectedController = controller;
        ShowMainWindow();
    }

    // Method to update the context menu dynamically
    private void UpdateContextMenu()
    {
        if (_trayIcon?.Menu is not { } menu)
        {
            return;
        }
        try
        {
            // Clear and rebuild the menu items
            menu.Items.Clear();

            // Show main window item
            NativeMenuItem showItem = new NativeMenuItem("Show")
            {
                Command = _viewModel.ShowMainWindowCommand
            };
            menu.Items.Add(showItem);

            // Add controller items if there are any controllers
            if (_controllers.Count != 0)
            {
                menu.Items.Add(new NativeMenuItemSeparator());

                foreach (ControllerViewModelBase controller in _controllers)
                {
                    NativeMenuItem controllerItem = new NativeMenuItem($"{controller.Name} - {controller.BatteryText}")
                    {
                        Command = new RelayCommand(() => SelectController(controller))
                    };
                    menu.Items.Add(controllerItem);
                }
            }

            // Separator before exit
            menu.Items.Add(new NativeMenuItemSeparator());

            // Exit item
            NativeMenuItem exitItem = new NativeMenuItem("Exit")
            {
                Command = _viewModel.ExitApplicationCommand
            };
            menu.Items.Add(exitItem);
        }
        catch (Exception ex)
        {
            Logger.Error<TrayIconService>($"Failed to update context menu: {ex.Message}");
        }
    }

    private void UpdateControllers()
    {
        if (_disposed)
        {
            return;
        }

        // Clear current controllers and unsubscribe from events
        foreach (ControllerViewModelBase controller in _controllers)
        {
            controller.PropertyChanged -= OnControllerPropertyChanged;
        }
        _controllers.Clear();

        // Add all controllers from the service
        foreach (ControllerViewModelBase controller in _selectedControllerService.AvailableControllers)
        {
            _controllers.Add(controller);
            // Subscribe to controller property changes (Battery percentage, name change..)
            controller.PropertyChanged += OnControllerPropertyChanged;
        }

        UpdateTrayIcon();
        UpdateContextMenu();
    }

    private void OnControllerPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (_disposed)
        {
            return;
        }

        if (e.PropertyName is nameof(ControllerViewModelBase.BatteryLevel) or nameof(ControllerViewModelBase.IsCharging) ||
            e.PropertyName == nameof(ControllerViewModelBase.BatteryIcon) ||
            e.PropertyName == nameof(ControllerViewModelBase.BatteryText) ||
            e.PropertyName == nameof(ControllerViewModelBase.Name))
        {
            // Update context menu to reflect new battery information
            UpdateTrayIcon();
            UpdateContextMenu();
        }
    }

    private void UpdateTrayIcon()
    {
        if (_trayIcon == null || _disposed)
        {
            return;
        }

        ControllerViewModelBase? selectedController = _selectedControllerService.SelectedController;

        if (selectedController != null)
        {
            // Controller selected
            try
            {
                // Update tray icon based on battery level of selected controller
                string iconPath = GetIconPathForBatteryLevel(selectedController);
                _trayIcon.Icon = new WindowIcon(AssetLoader.Open(new Uri(iconPath)));
            }
            catch (Exception ex)
            {
                Logger.Error<TrayIconService>($"Failed to update tray icon: {ex.Message}");

                // Fallback to default icon
                try
                {
                    string defaultIconPath = "avares://DualSenseClient/Assets/icon.ico";
                    _trayIcon.Icon = new WindowIcon(AssetLoader.Open(new Uri(defaultIconPath)));
                    _trayIcon.ToolTipText = "DualSense Client";
                }
                catch
                {
                    /* Ignore - can't do much if fallback fails */
                }
            }
        }
        else
        {
            // No controller selected, show default icon
            try
            {
                string defaultIconPath = "avares://DualSenseClient/Assets/icon.ico";
                _trayIcon.Icon = new WindowIcon(AssetLoader.Open(new Uri(defaultIconPath)));
                _trayIcon.ToolTipText = "DualSense Client";
            }
            catch (Exception ex)
            {
                Logger.Error<TrayIconService>($"Failed to reset tray icon: {ex.Message}");
            }
        }
    }

    private string GetIconPathForBatteryLevel(ControllerViewModelBase controller)
    {
        // TODO: Update icon based on battery level
        // Right now we're using app icon
        if (controller.IsCharging)
        {
            // Could return a charging icon if available
            return "avares://DualSenseClient/Assets/icon.ico";
        }

        // For different battery levels, you could return different icons
        // This is a placeholder implementation
        return "avares://DualSenseClient/Assets/icon.ico";
    }

    public void ShowMainWindow()
    {
        try
        {
            if (App.Desktop?.MainWindow == null)
            {
                return;
            }
            Window? mainWindow = App.Desktop.MainWindow;

            if (mainWindow == null)
            {
                return;
            }
            if (mainWindow.WindowState == WindowState.Minimized)
            {
                mainWindow.WindowState = WindowState.Normal;
            }

            mainWindow.Show();
            mainWindow.Activate();
            mainWindow.Focus();
        }
        catch (Exception ex)
        {
            Logger.Error<TrayIconService>($"Failed to show main window: {ex.Message}");
        }
    }

    public void HideMainWindow()
    {
        try
        {
            if (App.Desktop?.MainWindow == null)
            {
                return;
            }
            Window? mainWindow = App.Desktop.MainWindow;

            if (mainWindow == null)
            {
                return;
            }
            // Only hide if the close-to-tray setting is enabled
            if (_settingsManager.Application.Ui.CloseToTray)
            {
                mainWindow.Hide();
            }
        }
        catch (Exception ex)
        {
            Logger.Error<TrayIconService>($"Failed to hide main window: {ex.Message}");
        }
    }

    public bool ShouldCloseToTray()
    {
        return _settingsManager.Application.Ui.CloseToTray;
    }

    private void ExitApplication()
    {
        try
        {
            if (App.Desktop != null)
            {
                App.Desktop.Shutdown();
            }
        }
        catch (Exception ex)
        {
            Logger.Error<TrayIconService>($"Failed to exit application: {ex.Message}");
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        try
        {
            // Unsubscribe from controller events
            foreach (ControllerViewModelBase controller in _controllers)
            {
                controller.PropertyChanged -= OnControllerPropertyChanged;
            }

            _controllers.Clear();

            // Hide and dispose tray icon
            if (_trayIcon != null)
            {
                _trayIcon.IsVisible = false;
                _trayIcon.Dispose();
                _trayIcon = null;
            }

            Logger.Info<TrayIconService>("Tray icon service disposed successfully");
        }
        catch (Exception ex)
        {
            Logger.Error<TrayIconService>($"Error during disposal: {ex.Message}");
        }
    }
}