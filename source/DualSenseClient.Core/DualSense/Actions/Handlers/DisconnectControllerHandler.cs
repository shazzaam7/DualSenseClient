using DualSenseClient.Core.DualSense.Devices;
using DualSenseClient.Core.Logging;
using DualSenseClient.Core.Settings.Models;

namespace DualSenseClient.Core.DualSense.Actions.Handlers;

public class DisconnectControllerHandler : ISpecialActionHandler
{
    public void Execute(DualSenseController controller, SpecialActionSettings action)
    {
        Logger.Info<DisconnectControllerHandler>($"Disconnecting controller {action}");
        controller.DisconnectBluetooth();
    }
}