using System;
using System.Collections.ObjectModel;
using System.Linq;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using DualSenseClient.Core.Logging;
using DualSenseClient.Core.Settings;
using DualSenseClient.Core.Settings.Models;
using DualSenseClient.Services;
using NLog;
using Logger = DualSenseClient.Core.Logging.Logger;

namespace DualSenseClient.ViewModels.Pages;

public partial class SettingsPageViewModel : ViewModelBase
{
    public class ThemeItem
    {
        public AppTheme Theme { get; set; }
        public string DisplayName { get; set; } = string.Empty;
    }

    public class LogLevelItem
    {
        public LogLevel Level { get; set; } = LogLevel.Info;
        public string DisplayName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }

    // Properties
    private readonly ISettingsManager _settingsManager;
    private readonly ThemeService _themeService;

    public ObservableCollection<ThemeItem> AvailableThemes { get; } = [];
    [ObservableProperty] private ThemeItem? selectedTheme;

    public ObservableCollection<LogLevelItem> AvailableLogLevels { get; } = [];
    [ObservableProperty] private LogLevelItem? selectedLogLevel;

    // Constructor
    public SettingsPageViewModel(ISettingsManager settingsManager, ThemeService themeService)
    {
        _settingsManager = settingsManager;
        _themeService = themeService;

        InitializeThemes();
        InitializeLogLevels();

        ApplySettings(_settingsManager.Application);
        _settingsManager.SettingsChanged += OnSettingsChanged;
    }

    // Functions
    private void InitializeThemes()
    {
        // Add available themes from the ThemeService
        foreach (AppTheme theme in _themeService.GetAvailableThemes())
        {
            AvailableThemes.Add(new ThemeItem
            {
                Theme = theme,
                DisplayName = theme.ToString()
            });
        }
    }

    private void InitializeLogLevels()
    {
        AvailableLogLevels.Add(new LogLevelItem
        {
            Level = LogLevel.Trace,
            DisplayName = "Trace",
            Description = "Most detailed logging, includes all messages"
        });

        AvailableLogLevels.Add(new LogLevelItem
        {
            Level = LogLevel.Debug,
            DisplayName = "Debug",
            Description = "Detailed debugging information"
        });

        AvailableLogLevels.Add(new LogLevelItem
        {
            Level = LogLevel.Info,
            DisplayName = "Info",
            Description = "General informational messages"
        });

        AvailableLogLevels.Add(new LogLevelItem
        {
            Level = LogLevel.Warn,
            DisplayName = "Warning",
            Description = "Warning messages and recoverable errors"
        });

        AvailableLogLevels.Add(new LogLevelItem
        {
            Level = LogLevel.Error,
            DisplayName = "Error",
            Description = "Error messages only"
        });

        AvailableLogLevels.Add(new LogLevelItem
        {
            Level = LogLevel.Fatal,
            DisplayName = "Fatal",
            Description = "Only critical/fatal errors"
        });

        AvailableLogLevels.Add(new LogLevelItem
        {
            Level = LogLevel.Off,
            DisplayName = "Off",
            Description = "Disable logging completely"
        });
    }

    private void ApplySettings(ApplicationSettingsStore settings)
    {
        try
        {
            // Theme settings
            SelectedTheme = AvailableThemes.FirstOrDefault(t => t.Theme == settings.Ui.Theme);

            // Debug settings
            LogLevel logLevel = LogLevelHelper.FromString(settings.Debug.Logger.Level);
            SelectedLogLevel = AvailableLogLevels.FirstOrDefault(level => level.Level == logLevel);

            Logger.Info<SettingsPageViewModel>("Settings loaded");
            Logger.Info<SettingsPageViewModel>($"Theme: {settings.Ui.Theme}");
            Logger.Info<SettingsPageViewModel>($"Log Level: {settings.Debug.Logger.Level}");
        }
        catch (Exception)
        {
            // ignored
        }
    }

    private void OnSettingsChanged(object? sender, ApplicationSettingsStore settings)
    {
        Dispatcher.UIThread.Post(() =>
        {
            ApplySettings(settings);
        });
    }

    partial void OnSelectedThemeChanged(ThemeItem? value)
    {
        if (value == null)
        {
            return;
        }
        _settingsManager.Application.Ui.Theme = value.Theme;
        _themeService.SetTheme(value.Theme);
        SaveSettings();
        Logger.Info<SettingsPageViewModel>($"Theme changed to: {value.DisplayName}");
    }

    partial void OnSelectedLogLevelChanged(LogLevelItem? value)
    {
        if (value != null)
        {
            _settingsManager.Application.Debug.Logger.Level = LogLevelHelper.ToString(value.Level);
            Logger.SetLogLevel(value.Level);
            SaveSettings();
            Logger.Info<SettingsPageViewModel>($"Log level changed to: {value.DisplayName}");
        }
    }

    private void SaveSettings()
    {
        try
        {
            _settingsManager.SaveAll();
            Logger.Info<SettingsPageViewModel>("Settings saved successfully");
        }
        catch (Exception ex)
        {
            Logger.Error<SettingsPageViewModel>("Failed to save settings");
            Logger.LogExceptionDetails<SettingsPageViewModel>(ex);
        }
    }
}