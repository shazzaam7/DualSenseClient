using System;
using DualSenseClient.Core.Settings.Models;

namespace DualSenseClient.Core.DualSense.Events;

public class ProfileChangedEventArgs : EventArgs
{
    public string ControllerId { get; }
    public ControllerProfile Profile { get; }

    public ProfileChangedEventArgs(string controllerId, ControllerProfile profile)
    {
        ControllerId = controllerId;
        Profile = profile;
    }
}