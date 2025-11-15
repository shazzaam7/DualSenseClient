using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using DualSenseClient.Core.Logging;
using HidSharp;

namespace DualSenseClient.Core.Bluetooth;

internal class BluetoothHelper
{
    public static string? ExtractMacAddress(HidDevice device)
    {
        Logger.Trace<BluetoothHelper>("Attempting to extract MAC address from device");

        try
        {
            string? mac = TryFindBluetoothDevice() ?? TryFromSerial(device);
            Logger.Debug<BluetoothHelper>(mac != null ? $"Successfully extracted MAC address: {mac}" : "Failed to extract MAC address");
            return mac;
        }
        catch (Exception ex)
        {
            Logger.Error<BluetoothHelper>("Error extracting MAC address");
            Logger.LogExceptionDetails<BluetoothHelper>(ex, includeEnvironmentInfo: false);
            return null;
        }
    }

    private static string? TryFromSerial(HidDevice device)
    {
        Logger.Trace<BluetoothHelper>("Attempting to extract MAC from device serial number");

        try
        {
            string serial = device.GetSerialNumber();
            if (!string.IsNullOrEmpty(serial))
            {
                Logger.Trace<BluetoothHelper>($"Device serial: {serial}");
                Match match = Regex.Match(serial, @"([0-9A-Fa-f]{12})");
                if (match.Success)
                {
                    string mac = FormatMac(match.Groups[1].Value);
                    Logger.Debug<BluetoothHelper>($"Extracted MAC from serial: {mac}");
                    return mac;
                }
                Logger.Trace<BluetoothHelper>("Serial number doesn't contain valid MAC pattern");
            }
            else
            {
                Logger.Trace<BluetoothHelper>("Device has no serial number");
            }
        }
        catch (Exception ex)
        {
            Logger.Trace<BluetoothHelper>($"Could not get serial number: {ex.Message}");
        }
        return null;
    }

    private static string? TryFindBluetoothDevice()
    {
        Logger.Trace<BluetoothHelper>("Searching for DualSense controller via Bluetooth API");

        WindowsBluetooth.BLUETOOTH_FIND_RADIO_PARAMS radioParams = new WindowsBluetooth.BLUETOOTH_FIND_RADIO_PARAMS
        {
            dwSize = Marshal.SizeOf(typeof(WindowsBluetooth.BLUETOOTH_FIND_RADIO_PARAMS))
        };

        IntPtr radioHandle = IntPtr.Zero;
        IntPtr findHandle = WindowsBluetooth.BluetoothFindFirstRadio(ref radioParams, ref radioHandle);

        if (findHandle == IntPtr.Zero)
        {
            Logger.Trace<BluetoothHelper>("No Bluetooth radios found");
            return null;
        }

        Logger.Trace<BluetoothHelper>("Found Bluetooth radio, searching for DualSense devices");
        int radioCount = 0;
        int deviceCount = 0;

        do
        {
            radioCount++;
            Logger.Trace<BluetoothHelper>($"Scanning radio #{radioCount}");

            WindowsBluetooth.BLUETOOTH_DEVICE_SEARCH_PARAMS searchParams = new WindowsBluetooth.BLUETOOTH_DEVICE_SEARCH_PARAMS
            {
                dwSize = Marshal.SizeOf(typeof(WindowsBluetooth.BLUETOOTH_DEVICE_SEARCH_PARAMS)),
                fReturnAuthenticated = true,
                fReturnRemembered = true,
                fReturnConnected = true,
                hRadio = radioHandle
            };

            WindowsBluetooth.BLUETOOTH_DEVICE_INFO info = new WindowsBluetooth.BLUETOOTH_DEVICE_INFO
            {
                dwSize = Marshal.SizeOf(typeof(WindowsBluetooth.BLUETOOTH_DEVICE_INFO))
            };

            IntPtr devFind = WindowsBluetooth.BluetoothFindFirstDevice(ref searchParams, ref info);

            if (devFind != IntPtr.Zero)
            {
                do
                {
                    deviceCount++;
                    Logger.Trace<BluetoothHelper>($"Found Bluetooth device: {info.szName}");

                    if (!info.szName.Contains("DualSense", StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    byte[] bytes = BitConverter.GetBytes(info.Address);
                    string mac = FormatMacFromBytes(bytes);

                    Logger.Info<BluetoothHelper>($"Found DualSense controller via Bluetooth: {mac}");

                    WindowsBluetooth.BluetoothFindDeviceClose(devFind);
                    WindowsBluetooth.CloseHandle(radioHandle);
                    WindowsBluetooth.BluetoothFindRadioClose(findHandle);

                    return mac;
                } while (WindowsBluetooth.BluetoothFindNextDevice(devFind, ref info));

                WindowsBluetooth.BluetoothFindDeviceClose(devFind);
            }
            else
            {
                Logger.Trace<BluetoothHelper>("No devices found on this radio");
            }

            WindowsBluetooth.CloseHandle(radioHandle);
        } while (WindowsBluetooth.BluetoothFindNextRadio(findHandle, ref radioHandle));

        WindowsBluetooth.BluetoothFindRadioClose(findHandle);
        Logger.Debug<BluetoothHelper>($"Scanned {radioCount} radio(s), found {deviceCount} device(s), no DualSense controllers found");

        return null;
    }

    public static bool Disconnect(string mac)
    {
        Logger.Info<BluetoothHelper>($"Attempting to disconnect Bluetooth device: {mac}");

        try
        {
            byte[] btAddr = new byte[8];
            string[] parts = mac.Split(':');
            for (int i = 0; i < 6; i++) btAddr[5 - i] = Convert.ToByte(parts[i], 16);
            long addr = BitConverter.ToInt64(btAddr, 0);
            int bytesReturned = 0;

            WindowsBluetooth.BLUETOOTH_FIND_RADIO_PARAMS p = new WindowsBluetooth.BLUETOOTH_FIND_RADIO_PARAMS
            {
                dwSize = Marshal.SizeOf(typeof(WindowsBluetooth.BLUETOOTH_FIND_RADIO_PARAMS))
            };

            IntPtr hRadio = IntPtr.Zero;
            IntPtr search = WindowsBluetooth.BluetoothFindFirstRadio(ref p, ref hRadio);
            bool success = false;
            int radioAttempts = 0;

            while (!success && hRadio != IntPtr.Zero)
            {
                radioAttempts++;
                Logger.Trace<BluetoothHelper>($"Attempting disconnect on radio #{radioAttempts}");

                success = WindowsBluetooth.DeviceIoControl(hRadio, WindowsBluetooth.IOCTL_BTH_DISCONNECT_DEVICE,
                    ref addr, 8, IntPtr.Zero, 0, ref bytesReturned, IntPtr.Zero);

                WindowsBluetooth.CloseHandle(hRadio);

                if (!success && !WindowsBluetooth.BluetoothFindNextRadio(search, ref hRadio))
                {
                    hRadio = IntPtr.Zero;
                }
            }

            WindowsBluetooth.BluetoothFindRadioClose(search);

            if (success)
            {
                Logger.Info<BluetoothHelper>($"Successfully disconnected Bluetooth device: {mac}");
            }
            else
            {
                Logger.Error<BluetoothHelper>($"Failed to disconnect Bluetooth device: {mac} (tried {radioAttempts} radio(s))");
            }

            return success;
        }
        catch (Exception ex)
        {
            Logger.Error<BluetoothHelper>("Error while disconnecting Bluetooth device");
            Logger.LogExceptionDetails<BluetoothHelper>(ex, includeEnvironmentInfo: false);
            return false;
        }
    }

    private static string FormatMac(string mac)
    {
        string formatted = string.Join(":", Enumerable.Range(0, 6).Select(i => mac.Substring(i * 2, 2).ToUpper()));
        Logger.Trace<BluetoothHelper>($"Formatted MAC: {mac} -> {formatted}");
        return formatted;
    }

    private static string FormatMacFromBytes(byte[] bytes)
    {
        string formatted = $"{bytes[5]:X2}:{bytes[4]:X2}:{bytes[3]:X2}:{bytes[2]:X2}:{bytes[1]:X2}:{bytes[0]:X2}";
        Logger.Trace<BluetoothHelper>($"Formatted MAC from bytes: {formatted}");
        return formatted;
    }
}