using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using DualSenseClient.Core.Bluetooth;
using DualSenseClient.Core.DualSense.Enums;
using DualSenseClient.Core.DualSense.Reports;
using DualSenseClient.Core.Logging;
using DualSenseClient.Core.Utils;
using HidSharp;

namespace DualSenseClient.Core.DualSense.Devices;

/// <summary>
/// Represents a connected DualSense controller and manages input/output operations
/// </summary>
public class DualSenseController : IDisposable
{
    // Fields
    private readonly HidStream _stream;
    private readonly CancellationTokenSource _cts;
    private readonly Task _readTask;
    private readonly Lock _writeLock = new Lock();

    // Properties
    public HidDevice Device { get; }
    public ConnectionType ConnectionType { get; }
    public bool IsConnected { get; private set; } = true;
    public string? MacAddress { get; }

    // Current Controller State
    public BatteryState Battery { get; private set; } = new BatteryState();
    public LightbarColor CurrentLightbarColor { get; private set; } = new LightbarColor(0, 0, 255);
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
        Logger.Debug($"Initializing DualSense controller: {device.GetProductName()}");

        Device = device;
        _stream = stream;
        ConnectionType = device.GetMaxOutputReportLength() > 64 ? ConnectionType.Bluetooth : ConnectionType.USB;

        Logger.Info($"Controller mode detected: {ConnectionType}");
        Logger.Debug($"Max output report length: {device.GetMaxOutputReportLength()}");

        // Try to extract MAC address for both USB and Bluetooth
        MacAddress = BluetoothHelper.ExtractMacAddress(device);

        if (MacAddress != null)
        {
            Logger.Info($"Hardware MAC address: {MacAddress}");
        }
        else
        {
            Logger.Warning("Failed to extract MAC address from device");

            // For USB, try to get it from the controller itself via feature report
            MacAddress = TryGetMacAddressFromController();

            if (MacAddress != null)
            {
                Logger.Info($"MAC address from controller: {MacAddress}");
            }
        }

        _cts = new CancellationTokenSource();
        _readTask = Task.Run(() => ReadLoop(_cts.Token));

        Logger.Info("DualSense controller initialized successfully");
    }

    // Functions
    /// <summary>
    /// Attempts to retrieve MAC address directly from the controller via feature report (In case BluetoothHelper fails)
    /// </summary>
    private string? TryGetMacAddressFromController()
    {
        try
        {
            // DualSense stores MAC address in feature report 0x09
            byte[] report = new byte[20];
            report[0] = 0x09;

            _stream.GetFeature(report);

            // MAC address is at bytes 1-6
            if (report.Length >= 7)
            {
                string mac = $"{report[6]:X2}:{report[5]:X2}:{report[4]:X2}:{report[3]:X2}:{report[2]:X2}:{report[1]:X2}";
                Logger.Debug($"Extracted MAC from feature report: {mac}");
                return mac;
            }
        }
        catch (Exception ex)
        {
            Logger.Debug($"Could not retrieve MAC from feature report: {ex.Message}");
        }

        return null;
    }

    private async Task ReadLoop(CancellationToken ct)
    {
        Logger.Debug("Read loop started");
        byte[] buffer = new byte[128];
        int reportCount = 0;

        while (!ct.IsCancellationRequested && IsConnected)
        {
            try
            {
                int read = await _stream.ReadAsync(buffer, 0, buffer.Length, ct);

                if (read <= 0)
                {
                    Logger.Warning($"Read returned {read} bytes, disconnecting");
                    HandleDisconnection();
                    break;
                }

                reportCount++;
                Logger.Trace($"Report #{reportCount}: Read {read} bytes, ID: 0x{buffer[0]:X2}");

                ProcessInputReport(buffer, read);
            }
            catch (OperationCanceledException)
            {
                Logger.Debug("Read loop cancelled");
                break;
            }
            catch (IOException ioEx) when (IsDisconnectionException(ioEx))
            {
                // Expected disconnection - log at Info level without full details
                Logger.Info($"Controller disconnected: {ioEx.Message}");
                HandleDisconnection();
                break;
            }
            catch (Exception ex)
            {
                // Unexpected exception - log full details
                Logger.Error("Unexpected error in read loop");
                Logger.LogExceptionDetails(ex, includeEnvironmentInfo: false);
                HandleDisconnection();
                break;
            }
        }

        Logger.Debug("Read loop ended");
    }

    /// <summary>
    /// Determines if an exception is a normal disconnection event
    /// </summary>
    private static bool IsDisconnectionException(IOException ex)
    {
        // Check for common disconnection error messages
        string message = ex.Message.ToLowerInvariant();

        // HidSharp timeout/disconnection messages
        if (message.Contains("operation failed after some time") || message.Contains("device is not connected") || message.Contains("device not found") || message.Contains("no such device") || message.Contains("device removed"))
        {
            return true;
        }

        // Check inner exception for Win32 disconnection errors
        if (ex.InnerException is not Win32Exception win32Ex)
        {
            return false;
        }

        // ERROR_DEVICE_NOT_CONNECTED = 1167 (0x48F)
        // ERROR_NOT_READY = 21 (0x15)
        // ERROR_GEN_FAILURE = 31 (0x1F)
        return win32Ex.NativeErrorCode is 1167 or 21 or 31;
    }

    private void ProcessInputReport(byte[] data, int length)
    {
        byte reportId = data[0];

        if (ConnectionType == ConnectionType.Bluetooth)
        {
            switch (reportId)
            {
                case 0x31:
                    if (length < 10)
                    {
                        Logger.Warning($"Bluetooth report too short: {length} bytes (minimum 10)");
                        return;
                    }
                    byte[] stripped31 = new byte[length - 2];
                    Array.Copy(data, 2, stripped31, 0, length - 2);
                    Logger.Trace($"Stripped BT 0x31 headers: {length} -> {stripped31.Length} bytes");
                    ParseInputData(stripped31);
                    break;
                case 0x01:
                    // DualSense is in "simple" Bluetooth state (0x01)
                    // Sending out an "Output Report" resets it to the "normal" Bluetooth state (0x31)
                    Logger.Warning($"Controller is in simple Bluetooth state");
                    SendOutputReport();
                    break;
                default:
                    Logger.Warning($"Unknown Bluetooth report ID: 0x{reportId:X2}");
                    break;
            }
        }
        else
        {
            // USB mode (only 0x01 expected)
            if (reportId != 0x01)
            {
                Logger.Warning($"Invalid USB report ID: 0x{reportId:X2} (expected 0x01)");
                return;
            }

            byte[] strippedUsb = new byte[length - 1];
            Array.Copy(data, 1, strippedUsb, 0, length - 1);
            ParseInputData(strippedUsb);
        }
    }

    private void ParseInputData(byte[] data)
    {
        if (data.Length < 63)
        {
            Logger.Warning($"Data too short for parsing: {data.Length} bytes (expected 63)");
            return;
        }

        Logger.Trace("Parsing input data");

        // Analog sticks
        Input.LeftStickX = data[0];
        Input.LeftStickY = data[1];
        Input.RightStickX = data[2];
        Input.RightStickY = data[3];

        // Triggers
        Input.L2 = data[4];
        Input.R2 = data[5];

        // Buttons - Byte 7
        byte btnBlock1 = data[7];
        ParseDPadAndFaceButtons(btnBlock1);

        // Buttons - Byte 8
        byte btnBlock2 = data[8];
        ParseShoulderButtons(btnBlock2);

        // Buttons - Byte 9
        byte btnBlock3 = data[9];
        ParseSystemButtons(btnBlock3);

        // Gyro and Accelerometer
        if (data.Length > 26)
        {
            Input.GyroX = BitConverter.ToInt16(data, 15);
            Input.GyroZ = BitConverter.ToInt16(data, 17);
            Input.GyroY = BitConverter.ToInt16(data, 19);
            Input.AccelX = BitConverter.ToInt16(data, 21);
            Input.AccelY = BitConverter.ToInt16(data, 23);
            Input.AccelZ = BitConverter.ToInt16(data, 25);
        }

        // Touchpad
        if (data.Length > 40)
        {
            Input.Touch1 = ParseTouchData(data, 32);
            Input.Touch2 = ParseTouchData(data, 36);
        }

        // Battery
        if (data.Length > 52)
        {
            ParseBatteryInfo(data[52]);
        }

        // Connection status
        if (data.Length > 53)
        {
            ParseConnectionStatus(data[53]);
        }

        InputChanged?.Invoke(this, Input);
    }

    private void ParseDPadAndFaceButtons(byte btnBlock1)
    {
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
        Input.Square = (btnBlock1 & 0x10) != 0;
        Input.Cross = (btnBlock1 & 0x20) != 0;
        Input.Circle = (btnBlock1 & 0x40) != 0;
        Input.Triangle = (btnBlock1 & 0x80) != 0;

        Logger.Trace($"DPad: {dpadValue:X}, Face: Square={Input.Square}, Cross={Input.Cross}, Circle={Input.Circle}, Triangle={Input.Triangle}");
    }

    private void ParseShoulderButtons(byte btnBlock2)
    {
        Input.L1 = (btnBlock2 & 0x01) != 0;
        Input.R1 = (btnBlock2 & 0x02) != 0;
        Input.L2Button = (btnBlock2 & 0x04) != 0;
        Input.R2Button = (btnBlock2 & 0x08) != 0;
        Input.Create = (btnBlock2 & 0x10) != 0;
        Input.Options = (btnBlock2 & 0x20) != 0;
        Input.L3 = (btnBlock2 & 0x40) != 0;
        Input.R3 = (btnBlock2 & 0x80) != 0;

        Logger.Trace($"Shoulders: L1={Input.L1}, R1={Input.R1}, L2={Input.L2Button}, R2={Input.R2Button}, L3={Input.L3}, R3={Input.R3}");
    }

    private void ParseSystemButtons(byte btnBlock3)
    {
        Input.PS = (btnBlock3 & 0x01) != 0;
        Input.TouchPadClick = (btnBlock3 & 0x02) != 0;
        Input.Mute = (btnBlock3 & 0x04) != 0;

        Logger.Trace($"System: PS={Input.PS}, TouchPad={Input.TouchPadClick}, Mute={Input.Mute}");
    }

    private void ParseBatteryInfo(byte batteryByte)
    {
        byte rawLevel = (byte)(batteryByte & 0x0F);
        float newLevel = Math.Min((rawLevel * 100) / 8, 100);

        byte powerState = (byte)((batteryByte >> 4) & 0x0F);
        bool charging = powerState == 0x01;
        bool fullyCharged = powerState == 0x02;

        // Only log battery changes
        if (Math.Abs(Battery.BatteryLevel - newLevel) > 1 || Battery.IsCharging != charging || Battery.IsFullyCharged != fullyCharged)
        {
            Logger.Debug($"Battery: {newLevel:F0}%, State=0x{powerState:X} (Charging={charging}, Full={fullyCharged})");
        }

        Battery.BatteryLevel = newLevel;
        Battery.IsCharging = charging;
        Battery.IsFullyCharged = fullyCharged;
    }

    private void ParseConnectionStatus(byte statusByte)
    {
        Input.IsHeadphoneConnected = (statusByte & 0x01) != 0;
        Input.IsMicConnected = (statusByte & 0x02) != 0;
        Input.IsMicMuted = (statusByte & 0x04) != 0;
        Input.IsUsbDataConnected = (statusByte & 0x08) != 0;
        Input.IsUsbPowerConnected = (statusByte & 0x10) != 0;

        Logger.Trace($"Status: Headphone={Input.IsHeadphoneConnected}, Mic={Input.IsMicConnected}, USB={Input.IsUsbDataConnected}");
    }

    private TouchPoint ParseTouchData(byte[] data, int offset)
    {
        if (data.Length < offset + 4)
        {
            Logger.Trace($"Insufficient data for touchpad at offset {offset}");
            return new TouchPoint();
        }

        uint touchData = BitConverter.ToUInt32(data, offset);

        TouchPoint point = new TouchPoint
        {
            Index = (byte)(touchData & 0x7F),
            IsActive = ((touchData >> 7) & 0x01) == 0,
            X = (ushort)((touchData >> 8) & 0xFFF),
            Y = (ushort)((touchData >> 20) & 0xFFF)
        };

        if (point.IsActive)
        {
            Logger.Trace($"Touch at offset {offset}: ({point.X}, {point.Y}), Index={point.Index}");
        }

        return point;
    }

    /// <summary>
    /// Sets the lightbar color
    /// </summary>
    public bool SetLightbar(byte red, byte green, byte blue)
    {
        Logger.Debug($"Setting lightbar color: RGB({red}, {green}, {blue})");
        CurrentLightbarColor = new LightbarColor(red, green, blue);
        CurrentLightbarBehavior = LightbarBehavior.Custom;
        return SendOutputReport();
    }

    /// <summary>
    /// Sets the player LEDs
    /// </summary>
    public bool SetPlayerLeds(PlayerLed leds, PlayerLedBrightness brightness = PlayerLedBrightness.High)
    {
        Logger.Debug($"Setting player LEDs: {leds}, Brightness={brightness}");
        CurrentPlayerLeds = leds;
        CurrentPlayerLedBrightness = brightness;
        return SendOutputReport();
    }

    /// <summary>
    /// Sets the microphone LED
    /// </summary>
    public bool SetMicLed(MicLed led)
    {
        Logger.Debug($"Setting mic LED: {led}");
        CurrentMicLed = led;
        return SendOutputReport();
    }

    private bool SendOutputReport()
    {
        if (!IsConnected)
        {
            Logger.Warning("Cannot send output report: Controller not connected");
            return false;
        }

        lock (_writeLock)
        {
            try
            {
                byte[] report = ConnectionType == ConnectionType.Bluetooth ? BuildBluetoothOutputReport() : BuildUsbOutputReport();
                Logger.Trace($"Sending {ConnectionType} output report: {report.Length} bytes");

                _stream.Write(report);
                Logger.Trace("Output report sent successfully");
                return true;
            }
            catch (Exception ex)
            {
                Logger.Error("Failed to send output report");
                Logger.LogExceptionDetails(ex, includeEnvironmentInfo: false);
                HandleDisconnection();
                return false;
            }
        }
    }

    private byte[] BuildBluetoothOutputReport()
    {
        byte[] report = new byte[78];
        report[0] = 0x31; // BT Report ID
        report[1] = 0x02; // BT header flag

        // Feature mask
        report[2] = 0xFF;
        report[3] = 0xF7;

        // Lightbar
        report[41] = 0x02; // Enable lightbar modifications
        report[43] = (byte)CurrentLightbarBehavior;
        report[46] = CurrentLightbarColor.Red;
        report[47] = CurrentLightbarColor.Green;
        report[48] = CurrentLightbarColor.Blue;

        // Player LEDs
        report[44] = (byte)CurrentPlayerLedBrightness;
        report[45] = (byte)(0x20 | (byte)CurrentPlayerLeds);

        // Mic LED
        report[10] = (byte)CurrentMicLed;

        // Calculate and append CRC32
        uint crc = CRC32DualSense.Compute(report, 0, 74);
        report[74] = (byte)(crc & 0xFF);
        report[75] = (byte)((crc >> 8) & 0xFF);
        report[76] = (byte)((crc >> 16) & 0xFF);
        report[77] = (byte)((crc >> 24) & 0xFF);

        Logger.Trace($"BT Report - CRC: 0x{crc:X8}");

        return report;
    }

    private byte[] BuildUsbOutputReport()
    {
        byte[] report = new byte[48];
        report[0] = 0x02; // USB Report ID

        // Feature mask
        report[1] = 0xFF;
        report[2] = 0xF7;

        // Lightbar
        report[40] = 0x02; // Enable lightbar modifications
        report[42] = (byte)CurrentLightbarBehavior;
        report[45] = CurrentLightbarColor.Red;
        report[46] = CurrentLightbarColor.Green;
        report[47] = CurrentLightbarColor.Blue;

        // Player LEDs
        report[43] = (byte)CurrentPlayerLedBrightness;
        report[44] = (byte)(0x20 | (byte)CurrentPlayerLeds);

        // Mic LED
        report[9] = (byte)CurrentMicLed;

        return report;
    }

    /// <summary>
    /// Disconnects the Bluetooth connection (if connected via Bluetooth)
    /// </summary>
    public bool DisconnectBluetooth()
    {
        if (ConnectionType != ConnectionType.Bluetooth)
        {
            Logger.Warning("Cannot disconnect Bluetooth: Controller is connected via USB");
            return false;
        }

        if (MacAddress == null)
        {
            Logger.Warning("Cannot disconnect Bluetooth: MAC address not available");
            return false;
        }

        Logger.Info($"Disconnecting Bluetooth device: {MacAddress}");
        return BluetoothHelper.Disconnect(MacAddress);
    }

    private void HandleDisconnection()
    {
        if (!IsConnected)
        {
            Logger.Trace("HandleDisconnection called but already disconnected");
            return;
        }

        IsConnected = false;
        Logger.Info($"Controller disconnected: {Device.GetProductName()} ({ConnectionType})");

        try
        {
            Disconnected?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception ex)
        {
            Logger.Warning($"Exception in Disconnected event handler: {ex.Message}");
        }
    }

    public void Dispose()
    {
        Logger.Debug("Disposing DualSense controller");
        _cts.Cancel();

        try
        {
            if (!_readTask.Wait(1000))
            {
                Logger.Warning("Read task did not complete within timeout");
            }
        }
        catch (Exception ex)
        {
            Logger.Warning($"Exception while waiting for read task: {ex.Message}");
        }

        _cts.Dispose();
        _stream.Dispose();

        Logger.Debug("DualSense controller disposed");
    }
}