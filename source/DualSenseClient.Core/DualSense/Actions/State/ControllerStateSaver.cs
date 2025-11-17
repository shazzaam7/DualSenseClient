using DualSenseClient.Core.DualSense.Devices;
using DualSenseClient.Core.DualSense.Enums;
using DualSenseClient.Core.Logging;

namespace DualSenseClient.Core.DualSense.Actions.State;

public class ControllerStateSaver
{
    private readonly Dictionary<string, SavedControllerState> _savedStates = new Dictionary<string, SavedControllerState>();
    private readonly Dictionary<string, bool> _isSpecialActionActive = new Dictionary<string, bool>();

    private class SavedControllerState
    {
        public LightbarColor LightbarColor { get; set; } = new LightbarColor(0, 0, 255);
        public LightbarBehavior LightbarBehavior { get; set; }
        public PlayerLed PlayerLeds { get; set; }
        public PlayerLedBrightness PlayerLedBrightness { get; set; }
    }

    public void SaveState(string controllerId, DualSenseController controller)
    {
        _savedStates[controllerId] = new SavedControllerState
        {
            LightbarColor = new LightbarColor(
                controller.CurrentLightbarColor.Red,
                controller.CurrentLightbarColor.Green,
                controller.CurrentLightbarColor.Blue
            ),
            LightbarBehavior = controller.CurrentLightbarBehavior,
            PlayerLeds = controller.CurrentPlayerLeds,
            PlayerLedBrightness = controller.CurrentPlayerLedBrightness
        };

        _isSpecialActionActive[controllerId] = true;
        Logger.Info<ControllerStateSaver>($"Controller {controllerId}: Stored original LED state");
    }

    public void RestoreState(string controllerId, DualSenseController controller)
    {
        if (_savedStates.TryGetValue(controllerId, out SavedControllerState? state))
        {
            controller.SetLightbar(state.LightbarColor.Red, state.LightbarColor.Green, state.LightbarColor.Blue);
            controller.SetLightbarBehavior(state.LightbarBehavior);
            controller.SetPlayerLeds(state.PlayerLeds, state.PlayerLedBrightness);

            _savedStates.Remove(controllerId);
            _isSpecialActionActive[controllerId] = false;

            Logger.Info<ControllerStateSaver>($"Controller {controllerId}: Restored original LED state");
        }
    }

    public void ResetState(string controllerId)
    {
        if (_savedStates.ContainsKey(controllerId))
        {
            _savedStates.Remove(controllerId);
        }

        if (_isSpecialActionActive.ContainsKey(controllerId))
        {
            _isSpecialActionActive[controllerId] = false;
        }

        Logger.Info<ControllerStateSaver>($"Controller {controllerId}: Reset special action state");
    }

    public bool IsSpecialActionActive(string controllerId)
    {
        return _isSpecialActionActive.GetValueOrDefault(controllerId, false);
    }
}