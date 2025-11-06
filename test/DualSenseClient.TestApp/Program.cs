using System;
using DualSenseClient.Core.DualSense;
using DualSenseClient.Core.Logging;

namespace DualSenseClient.TestApp
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            using var manager = new DualSenseManager();

            // Subscribe to events
            manager.ControllerConnected += (s, controller) =>
            {
                Logger.Info($"Controller connected: {(controller.ConnectionType)}");

                // Subscribe to input
                controller.InputChanged += (sender, input) =>
                {
                    List<string> parts = new List<string>();

                    // Analog sticks
                    if (Math.Abs(input.LeftStickX - 128) > 10 || Math.Abs(input.LeftStickY - 128) > 10)
                    {
                        parts.Add($"LS({input.LeftStickX},{input.LeftStickY})");
                    }

                    if (Math.Abs(input.RightStickX - 128) > 10 || Math.Abs(input.RightStickY - 128) > 10)
                    {
                        parts.Add($"RS({input.RightStickX},{input.RightStickY})");
                    }

                    // Triggers
                    if (input.L2 > 0) parts.Add($"L2:{input.L2}");
                    if (input.R2 > 0) parts.Add($"R2:{input.R2}");

                    // Buttons
                    if (input.Cross) parts.Add("Cross");
                    if (input.Circle) parts.Add("Circle");
                    if (input.Square) parts.Add("Square");
                    if (input.Triangle) parts.Add("Triangle");
                    if (input.DPadUp) parts.Add("Up");
                    if (input.DPadDown) parts.Add("Down");
                    if (input.DPadLeft) parts.Add("Left");
                    if (input.DPadRight) parts.Add("Right");
                    if (input.L1) parts.Add("L1");
                    if (input.R1) parts.Add("R1");
                    if (input.L3) parts.Add("L3");
                    if (input.R3) parts.Add("R3");
                    if (input.Create) parts.Add("Create");
                    if (input.Options) parts.Add("Options");
                    if (input.PS) parts.Add("PS");
                    if (input.TouchPadClick) parts.Add("Touchpad");
                    if (input.Mute) parts.Add("Mute");

                    // Touchpad
                    if (input.Touch1.IsActive)
                    {
                        parts.Add($"Touch1({input.Touch1.X},{input.Touch1.Y})");
                    }
                    if (input.Touch2.IsActive)
                    {
                        parts.Add($"Touch2({input.Touch2.X},{input.Touch2.Y})");
                    }

                    // Gyro/Accel (only show if significant movement)
                    if (Math.Abs(input.GyroX) > 100 || Math.Abs(input.GyroY) > 100 || Math.Abs(input.GyroZ) > 100)
                    {
                        parts.Add($"Gyro({input.GyroX},{input.GyroY},{input.GyroZ})");
                    }

                    // Battery info
                    string batteryIcon = controller.Battery.IsCharging ? "⚡" :
                        controller.Battery.IsFullyCharged ? "✓" : "";
                    parts.Add($"Bat:{controller.Battery.BatteryLevel}%{batteryIcon}");

                    // Connection status
                    List<string> status = new List<string>();
                    if (input.IsHeadphoneConnected) status.Add("Headset");
                    if (input.IsMicConnected) status.Add("Microphone");
                    if (input.IsUsbDataConnected) status.Add("USB");

                    if (status.Count > 0)
                    {
                        parts.Add($"[{string.Join(" ", status)}]");
                    }

                    // Build output string
                    string output = string.Join(" | ", parts);

                    // Pad to clear previous line content
                    output = output.PadRight(220);

                    // Use carriage return to overwrite the same line
                    Console.WriteLine($"\r{output}");
                };
            };
        }
    }
}