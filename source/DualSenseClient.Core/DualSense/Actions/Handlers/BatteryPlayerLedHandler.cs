using DualSenseClient.Core.DualSense.Devices;
using DualSenseClient.Core.DualSense.Enums;
using DualSenseClient.Core.Logging;
using DualSenseClient.Core.Settings.Models;

namespace DualSenseClient.Core.DualSense.Actions.Handlers;

public class BatteryPlayerLedHandler : ISpecialActionHandler
{
    public void Execute(DualSenseController controller, SpecialActionSettings action)
    {
        float level = controller.Battery.BatteryLevel;
        PlayerLed leds = PlayerLed.None;
        
        if (level >= 12.5f) leds |= PlayerLed.LED_1;
        if (level >= 25f) leds |= PlayerLed.LED_2;
        if (level >= 50f) leds |= PlayerLed.LED_3;
        if (level >= 75f) leds |= PlayerLed.LED_4;
        if (level >= 100f) leds |= PlayerLed.LED_5;

        controller.SetPlayerLeds(leds);

        Logger.Info<BatteryPlayerLedHandler>(
            $"Battery indicator: LEDs={leds}, Battery={level:F0}%"
        );
    }
}