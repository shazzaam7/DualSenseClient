using DualSenseClient.Core.Bluetooth;
using DualSenseClient.Core.DualSense.Enums;
using DualSenseClient.Core.DualSense.Reports;
using DualSenseClient.Core.Logging;
using DualSenseClient.Core.Utils;
using HidSharp;

namespace DualSenseClient.Core.DualSense.Devices;

public class DualSenseController : IDisposable
{
    private readonly HidStream _stream;
    private readonly CancellationTokenSource _cts;
    private readonly Task _readTask;
    private Lock _writeLock = new Lock();

    // Properties
    public HidDevice Device { get; }
    public bool IsBluetooth { get; }
    public bool IsConnected { get; private set; } = true;
    public string? MacAddress { get; }

    // State
    public BatteryState Battery { get; private set; } = new BatteryState();
    public LightbarColor CurrentLightbarColor { get; private set; }
    public LightbarBehavior CurrentLightbarBehavior { get; private set; }
    public PlayerLed CurrentPlayerLeds { get; private set; }
    public PlayerLedBrightness CurrentPlayerLedBrightness { get; private set; }
    public MicLed CurrentMicLed { get; private set; }
    public InputState Input { get; private set; } = new InputState();

    // Events
    public event EventHandler<InputState>? InputChanged;
    public event EventHandler? Disconnected;

    // Constructor
    public DualSenseController(HidDevice device, HidStream stream)
    {
        Device = device;
        _stream = stream;
        IsBluetooth = device.GetMaxOutputReportLength() > 64;

        if (IsBluetooth)
        {
            MacAddress = BluetoothHelper.ExtractMacAddress(device);
        }

        _cts = new CancellationTokenSource();
        _readTask = Task.Run(() => ReadLoop(_cts.Token));
    }

    private async Task ReadLoop(CancellationToken ct)
    {
        byte[] buffer = new byte[128];

        while (!ct.IsCancellationRequested && IsConnected)
        {
            try
            {
                int read = await _stream.ReadAsync(buffer, 0, buffer.Length, ct);
                if (read <= 0)
                {
                    HandleDisconnection();
                    break;
                }

                ProcessInputReport(buffer, read);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                Logger.Error("Read error");
                Logger.LogExceptionDetails(ex);
                HandleDisconnection();
                break;
            }
        }
    }

    private void ProcessInputReport(byte[] data, int length)
    {
        byte reportId = data[0];

        // Strip report ID and header, then parse
        byte[] strippedData;
        if (IsBluetooth)
        {
            // Bluetooth: validate and strip
            if (reportId != 0x31 || length < 10)
            {
                return;
            }

            strippedData = new byte[length - 2];
            Array.Copy(data, 2, strippedData, 0, length - 2);
        }
        else
        {
            // USB: validate and strip
            if (reportId != 0x01 || length < 10)
            {
                return;
            }

            strippedData = new byte[length - 1];
            Array.Copy(data, 1, strippedData, 0, length - 1);
        }

        ParseInputData(strippedData);
    }

    private void ParseInputData(byte[] data)
    {
        // We should have USBGetStateData (63 bytes) after stripping headers
        if (data.Length < 63)
        {
            return;
        }

        // Bytes 0-3: Analog sticks
        Input.LeftStickX = data[0];
        Input.LeftStickY = data[1];
        Input.RightStickX = data[2];
        Input.RightStickY = data[3];

        // Bytes 4-5: Triggers (Hold Status)
        Input.L2 = data[4]; // TriggerLeft
        Input.R2 = data[5]; // TriggerRight

        // Byte 6: SeqNo (sequence number, always 0x01 on BT)

        // Byte 7: DPad (bits 0-3) + Face buttons (bits 4-7)
        byte btnBlock1 = data[7];

        // DPad (lower 4 bits)
        byte dpadValue = (byte)(btnBlock1 & 0x0F);
        Input.DPadUp = dpadValue == 0x00;
        Input.DPadRight = dpadValue == 0x02;
        Input.DPadDown = dpadValue == 0x04;
        Input.DPadLeft = dpadValue == 0x06;

        // Diagonals: Up-Right=1, Down-Right=3, Down-Left=5, Up-Left=7
        Input.DPadUp = Input.DPadUp || dpadValue == 0x01 || dpadValue == 0x07;
        Input.DPadRight = Input.DPadRight || dpadValue == 0x01 || dpadValue == 0x03;
        Input.DPadDown = Input.DPadDown || dpadValue == 0x03 || dpadValue == 0x05;
        Input.DPadLeft = Input.DPadLeft || dpadValue == 0x05 || dpadValue == 0x07;

        // Face buttons (upper 4 bits)
        Input.Square = (btnBlock1 & 0x10) != 0; // bit 4
        Input.Cross = (btnBlock1 & 0x20) != 0; // bit 5
        Input.Circle = (btnBlock1 & 0x40) != 0; // bit 6
        Input.Triangle = (btnBlock1 & 0x80) != 0; // bit 7

        // Byte 8: Shoulder buttons and stick clicks
        byte btnBlock2 = data[8];

        Input.L1 = (btnBlock2 & 0x01) != 0; // bit 0
        Input.R1 = (btnBlock2 & 0x02) != 0; // bit 1
        Input.L2Button = (btnBlock2 & 0x04) != 0; // bit 2
        Input.R2Button = (btnBlock2 & 0x08) != 0; // bit 3
        Input.Create = (btnBlock2 & 0x10) != 0; // bit 4 (ButtonCreate/Share)
        Input.Options = (btnBlock2 & 0x20) != 0; // bit 5
        Input.L3 = (btnBlock2 & 0x40) != 0; // bit 6
        Input.R3 = (btnBlock2 & 0x80) != 0; // bit 7

        // Byte 9: System buttons and Edge buttons
        byte btnBlock3 = data[9];

        Input.PS = (btnBlock3 & 0x01) != 0; // bit 0 (ButtonHome)
        Input.TouchPadClick = (btnBlock3 & 0x02) != 0; // bit 1 (ButtonPad)
        Input.Mute = (btnBlock3 & 0x04) != 0; // bit 2 (ButtonMute)
        // bits 3-7 are for DualSense Edge buttons and unknown

        // Bytes 15-26: Gyro and Accelerometer (int16 values)
        if (data.Length > 26)
        {
            Input.GyroX = BitConverter.ToInt16(data, 15); // AngularVelocityX
            Input.GyroZ = BitConverter.ToInt16(data, 17); // AngularVelocityZ
            Input.GyroY = BitConverter.ToInt16(data, 19); // AngularVelocityY
            Input.AccelX = BitConverter.ToInt16(data, 21); // AccelerometerX
            Input.AccelY = BitConverter.ToInt16(data, 23); // AccelerometerY
            Input.AccelZ = BitConverter.ToInt16(data, 25); // AccelerometerZ
        }

        // Bytes 32-40: TouchData (9 bytes total)
        if (data.Length > 40)
        {
            // TouchData contains 2 TouchFingerData (4 bytes each) + 1 timestamp byte
            Input.Touch1 = ParseTouchData(data, 32);
            Input.Touch2 = ParseTouchData(data, 36);
        }

        // Byte 52: Battery info
        if (data.Length > 52)
        {
            byte batteryByte = data[52];

            // Lower 4 bits: PowerPercent
            byte rawLevel = (byte)(batteryByte & 0x0F);
            Battery.BatteryLevel = (rawLevel * 100) / 8; // Convert 0-8 to 0-100%
            Battery.BatteryLevel = Math.Min(Battery.BatteryLevel, 100); // Cap at 100%

            // Upper 4 bits: PowerState
            byte powerState = (byte)((batteryByte >> 4) & 0x0F);
            Battery.IsCharging = powerState == 0x01; // Charging
            Battery.IsFullyCharged = powerState == 0x02; // Complete/Full
        }

        // Byte 53: Connection and audio status
        if (data.Length > 53)
        {
            byte statusByte = data[53];
            Input.IsHeadphoneConnected = (statusByte & 0x01) != 0; // PluggedHeadphones
            Input.IsMicConnected = (statusByte & 0x02) != 0; // PluggedMic
            Input.IsMicMuted = (statusByte & 0x04) != 0; // MicMuted
            Input.IsUsbDataConnected = (statusByte & 0x08) != 0; // PluggedUsbData
            Input.IsUsbPowerConnected = (statusByte & 0x10) != 0; // PluggedUsbPower
        }

        InputChanged?.Invoke(this, Input);
    }

    private TouchPoint ParseTouchData(byte[] data, int offset)
    {
        // TouchFingerData is 4 bytes
        if (data.Length < offset + 4)
        {
            return new TouchPoint();
        }

        uint touchData = BitConverter.ToUInt32(data, offset);

        return new TouchPoint
        {
            Index = (byte)(touchData & 0x7F), // bits 0-6
            IsActive = ((touchData >> 7) & 0x01) == 0, // bit 7 (NotTouching inverted)
            X = (ushort)((touchData >> 8) & 0xFFF), // bits 8-19 (12 bits)
            Y = (ushort)((touchData >> 20) & 0xFFF) // bits 20-31 (12 bits)
        };
    }

    public bool SetLightbar(byte red, byte green, byte blue)
    {
        CurrentLightbarColor = new LightbarColor(red, green, blue);
        CurrentLightbarBehavior = LightbarBehavior.Custom;
        return SendOutputReport();
    }

    public bool SetPlayerLeds(PlayerLed leds, PlayerLedBrightness brightness = PlayerLedBrightness.High)
    {
        CurrentPlayerLeds = leds;
        CurrentPlayerLedBrightness = brightness;
        return SendOutputReport();
    }

    public bool SetMicLed(MicLed led)
    {
        CurrentMicLed = led;
        return SendOutputReport();
    }

    private bool SendOutputReport()
    {
        if (!IsConnected)
        {
            return false;
        }

        lock (_writeLock)
        {
            try
            {
                byte[] report;

                if (IsBluetooth)
                {
                    report = new byte[78];
                    report[0] = 0x31; // BT Report ID
                    report[1] = 0x02; // BT header flag

                    // Feature mask
                    report[2] = 0xFF;
                    report[3] = 0xF7;

                    // Lightbar
                    report[41] = 0x02; // Enable lightbar mods
                    report[43] = (byte)CurrentLightbarBehavior;
                    report[46] = CurrentLightbarColor.Red;
                    report[47] = CurrentLightbarColor.Green;
                    report[48] = CurrentLightbarColor.Blue;

                    // Player LEDs
                    report[44] = (byte)CurrentPlayerLedBrightness;
                    report[45] = (byte)(0x20 | (byte)CurrentPlayerLeds);

                    // Mic LED
                    report[11] = (byte)CurrentMicLed;

                    // Calculate CRC32
                    uint crc = CRC32DualSense.Compute(report, 0, 74);
                    report[74] = (byte)(crc & 0xFF);
                    report[75] = (byte)((crc >> 8) & 0xFF);
                    report[76] = (byte)((crc >> 16) & 0xFF);
                    report[77] = (byte)((crc >> 24) & 0xFF);
                }
                else
                {
                    report = new byte[48];
                    report[0] = 0x02; // USB Report ID

                    // Feature mask
                    report[1] = 0xFF;
                    report[2] = 0xF7;

                    // Lightbar
                    report[40] = 0x02; // Enable lightbar mods
                    report[42] = (byte)CurrentLightbarBehavior;
                    report[45] = CurrentLightbarColor.Red;
                    report[46] = CurrentLightbarColor.Green;
                    report[47] = CurrentLightbarColor.Blue;

                    // Player LEDs
                    report[43] = (byte)CurrentPlayerLedBrightness;
                    report[44] = (byte)(0x20 | (byte)CurrentPlayerLeds);

                    // Mic LED
                    report[10] = (byte)CurrentMicLed;
                }

                _stream.Write(report);
                return true;
            }
            catch (Exception ex)
            {
                Logger.Error("Failed to send output report");
                Logger.LogExceptionDetails(ex);
                HandleDisconnection();
                return false;
            }
        }
    }

    private void HandleDisconnection()
    {
        if (!IsConnected)
        {
            return;
        }

        IsConnected = false;
        Logger.Info("Controller disconnected");
        Disconnected?.Invoke(this, EventArgs.Empty);
    }

    public void Dispose()
    {
        _cts?.Cancel();
        try
        {
            _readTask?.Wait(1000);
        }
        catch
        {
            // ignored
        }

        _cts?.Dispose();
        _stream?.Dispose();
    }
}