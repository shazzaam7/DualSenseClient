using DualSenseClient.Core.DualSense.Actions.Handlers;
using DualSenseClient.Core.DualSense.Actions.State;
using DualSenseClient.Core.DualSense.Devices;
using DualSenseClient.Core.DualSense.Enums;
using DualSenseClient.Core.DualSense.Events;
using DualSenseClient.Core.Settings;
using DualSenseClient.Core.Settings.Models;

namespace DualSenseClient.Core.DualSense.Actions;

public class SpecialActionService
{
    private readonly ISettingsManager _settingsManager;
    private readonly DualSenseProfileManager _profileManager;
    private readonly ControllerStateTracker _stateTracker;
    private readonly ControllerStateSaver _stateSaver;
    private readonly SpecialActionFactory _actionFactory;

    public SpecialActionService(ISettingsManager settingsManager, DualSenseProfileManager profileManager)
    {
        _settingsManager = settingsManager;
        _profileManager = profileManager;
        _stateTracker = new ControllerStateTracker();
        _stateSaver = new ControllerStateSaver();
        _actionFactory = new SpecialActionFactory();

        // Subscribe to profile changes to update special actions when a profile changes
        _profileManager.ProfileChanged += OnProfileChanged;
    }

    private void OnProfileChanged(object? sender, ProfileChangedEventArgs e)
    {
        string controllerId = e.ControllerId;

        // Reset button states to clear any held combinations from the old profile
        _stateTracker.ResetButtonStates(controllerId);

        // Reset the state saver completely for this controller ID
        _stateSaver.ResetState(controllerId);
    }

    private string NormalizeMacAddress(string macAddress)
    {
        return macAddress?.Replace(":", "") ?? "";
    }

    public void ProcessButtonEvent(DualSenseController controller, ButtonType button, bool isPressed)
    {
        string controllerId = GetControllerId(controller);

        // Check for timeout and restore state if needed
        if (_stateTracker.HasTimedOut(controllerId))
        {
            if (_stateSaver.IsSpecialActionActive(controllerId))
            {
                _stateSaver.RestoreState(controllerId, controller);
            }
            _stateTracker.ResetButtonStates(controllerId);
        }

        // Update button state
        _stateTracker.UpdateButtonState(controllerId, button, isPressed);

        // Check for special actions
        CheckForSpecialActions(controller);
    }

    public void CheckForActiveSpecialActionRelease(DualSenseController controller)
    {
        string controllerId = GetControllerId(controller);

        if (_stateSaver.IsSpecialActionActive(controllerId))
        {
            _stateSaver.RestoreState(controllerId, controller);
        }
    }

    private void CheckForSpecialActions(DualSenseController controller)
    {
        string controllerId = GetControllerId(controller);

        // Check profile-specific actions first
        ControllerProfile? profile = _profileManager.GetControllerProfile(controllerId);
        if (profile.SpecialActions is not { Count: > 0 })
        {
            return;
        }

        // Sort actions by combination length in descending order to prioritize longer combinations
        List<SpecialActionSettings> sortedActions = profile.SpecialActions
            .OrderByDescending(action => action.Combination.Buttons.Count)
            .ToList();

        foreach (SpecialActionSettings action in sortedActions.Where(action => _stateTracker.IsButtonCombinationHeld(controllerId, action.Combination)))
        {
            EnsureStateSaved(controllerId, controller);
            ExecuteSpecialAction(controller, action);
            return; // Execute only first matching action
        }
    }

    private void ExecuteSpecialAction(DualSenseController controller, SpecialActionSettings action)
    {
        ISpecialActionHandler? handler = _actionFactory.GetHandler(action);
        handler?.Execute(controller, action);
    }

    private void EnsureStateSaved(string controllerId, DualSenseController controller)
    {
        if (!_stateSaver.IsSpecialActionActive(controllerId))
        {
            _stateSaver.SaveState(controllerId, controller);
        }
    }

    private string GetControllerId(DualSenseController controller)
    {
        if (controller.MacAddress != null)
        {
            return NormalizeMacAddress(controller.MacAddress);
        }
        return controller.Device.DevicePath;
    }
}