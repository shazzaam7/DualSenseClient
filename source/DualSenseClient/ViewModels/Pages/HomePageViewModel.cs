using System;
using CommunityToolkit.Mvvm.ComponentModel;
using DualSenseClient.Core.DualSense;
using DualSenseClient.Core.Settings;
using DualSenseClient.Core.Settings.Models;
using DualSenseClient.Services;
using DualSenseClient.ViewModels.Controls;
using DualSenseClient.Core.Logging;

namespace DualSenseClient.ViewModels.Pages;

public partial class HomePageViewModel : ViewModelBase
{
    public HomePageViewModel()
    {
        Logger.Debug("Creating HomePageViewModel");

        Logger.Debug("HomePageViewModel created successfully");
    }
}