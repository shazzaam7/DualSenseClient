using DualSenseClient.Core.DualSense.Devices;
using DualSenseClient.Core.Logging;
using DualSenseClient.Core.Settings.Models;

namespace DualSenseClient.Core.DualSense.Actions.Handlers;

public class BatteryLightbarHandler : ISpecialActionHandler
{
    public void Execute(DualSenseController controller, SpecialActionSettings action)
    {
        float level = Math.Clamp(controller.Battery.BatteryLevel / 100f, 0f, 1f);

        byte red = (byte)(255 * (1f - level));
        byte green = (byte)(255 * level);
        byte blue = 0;

        controller.SetLightbar(red, green, blue);

        Logger.Info<BatteryLightbarHandler>($"Battery indicator: RGB({red},{green},{blue}) - Battery: {controller.Battery.BatteryLevel}%");
    }
}