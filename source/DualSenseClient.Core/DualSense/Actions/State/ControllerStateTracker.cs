using DualSenseClient.Core.DualSense.Enums;
using DualSenseClient.Core.Settings.Models;

namespace DualSenseClient.Core.DualSense.Actions.State;

public class ControllerStateTracker
{
    private readonly Dictionary<string, Dictionary<ButtonType, bool>> _buttonStates = new();
    private readonly Dictionary<string, DateTime> _lastButtonPress = new();
    private const double BUTTON_COMBINATION_TIMEOUT = 1.0;

    public void UpdateButtonState(string controllerId, ButtonType button, bool isPressed)
    {
        if (!_buttonStates.ContainsKey(controllerId))
        {
            _buttonStates[controllerId] = new Dictionary<ButtonType, bool>();
        }

        _buttonStates[controllerId][button] = isPressed;
        _lastButtonPress[controllerId] = DateTime.Now;
    }

    public bool HasTimedOut(string controllerId)
    {
        if (!_lastButtonPress.ContainsKey(controllerId))
        {
            return true;
        }

        return (DateTime.Now - _lastButtonPress[controllerId]).TotalSeconds > BUTTON_COMBINATION_TIMEOUT;
    }

    public void ResetButtonStates(string controllerId)
    {
        if (_buttonStates.ContainsKey(controllerId))
        {
            foreach (ButtonType button in _buttonStates[controllerId].Keys.ToList())
            {
                _buttonStates[controllerId][button] = false;
            }
        }
    }

    public Dictionary<ButtonType, bool> GetButtonStates(string controllerId)
    {
        return _buttonStates.GetValueOrDefault(controllerId, new Dictionary<ButtonType, bool>());
    }

    public bool IsButtonCombinationHeld(string controllerId, ButtonCombination combination)
    {
        if (combination.IsEmpty)
        {
            return false;
        }

        Dictionary<ButtonType, bool> buttonStates = GetButtonStates(controllerId);

        // All buttons in the combination must be pressed
        return combination.Buttons.All(button => buttonStates.GetValueOrDefault(button, false));
    }

    public bool AreButtonsHeld(string controllerId, params ButtonType[] buttons)
    {
        if (buttons.Length == 0)
        {
            return false;
        }

        Dictionary<ButtonType, bool> buttonStates = GetButtonStates(controllerId);
        return buttons.All(button => buttonStates.GetValueOrDefault(button, false));
    }
}