using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using DualSenseClient.Core.Logging;
using HidSharp;

namespace DualSenseClient.Core.Bluetooth;

internal static class BluetoothHelper
{
    public static string? ExtractMacAddress(HidDevice device)
    {
        try
        {
            string? mac = TryFindBluetoothDevice() ?? TryFromSerial(device);
            return mac;
        }
        catch (Exception ex)
        {
            Logger.Error($"Error extracting MAC");
            Logger.LogExceptionDetails(ex);
            return null;
        }
    }

    private static string? TryFromSerial(HidDevice device)
    {
        try
        {
            string serial = device.GetSerialNumber();
            if (!string.IsNullOrEmpty(serial))
            {
                Match match = Regex.Match(serial, @"([0-9A-Fa-f]{12})");
                if (match.Success)
                {
                    return FormatMac(match.Groups[1].Value);
                }
            }
        }
        catch
        {
            // ignored
        }
        return null;
    }

    private static string? TryFindBluetoothDevice()
    {
        WindowsBluetooth.BLUETOOTH_FIND_RADIO_PARAMS radioParams = new WindowsBluetooth.BLUETOOTH_FIND_RADIO_PARAMS
        {
            dwSize = Marshal.SizeOf(typeof(WindowsBluetooth.BLUETOOTH_FIND_RADIO_PARAMS))
        };

        IntPtr radioHandle = IntPtr.Zero;
        IntPtr findHandle = WindowsBluetooth.BluetoothFindFirstRadio(ref radioParams, ref radioHandle);

        if (findHandle == IntPtr.Zero)
        {
            return null;
        }

        do
        {
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
                    if (!info.szName.Contains("DualSense", StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }
                    byte[] bytes = BitConverter.GetBytes(info.Address);
                    WindowsBluetooth.BluetoothFindDeviceClose(devFind);
                    WindowsBluetooth.CloseHandle(radioHandle);
                    WindowsBluetooth.BluetoothFindRadioClose(findHandle);
                    return FormatMacFromBytes(bytes);
                } while (WindowsBluetooth.BluetoothFindNextDevice(devFind, ref info));

                WindowsBluetooth.BluetoothFindDeviceClose(devFind);
            }

            WindowsBluetooth.CloseHandle(radioHandle);
        } while (WindowsBluetooth.BluetoothFindNextRadio(findHandle, ref radioHandle));

        WindowsBluetooth.BluetoothFindRadioClose(findHandle);
        return null;
    }

    public static bool Disconnect(string mac)
    {
        try
        {
            byte[] btAddr = new byte[8];
            string[] parts = mac.Split(':');
            for (int i = 0; i < 6; i++) btAddr[5 - i] = Convert.ToByte(parts[i], 16);
            long addr = BitConverter.ToInt64(btAddr, 0);
            int bytesReturned = 0;

            WindowsBluetooth.BLUETOOTH_FIND_RADIO_PARAMS p = new WindowsBluetooth.BLUETOOTH_FIND_RADIO_PARAMS { dwSize = Marshal.SizeOf(typeof(WindowsBluetooth.BLUETOOTH_FIND_RADIO_PARAMS)) };

            IntPtr hRadio = IntPtr.Zero;
            IntPtr search = WindowsBluetooth.BluetoothFindFirstRadio(ref p, ref hRadio);
            bool success = false;

            while (!success && hRadio != IntPtr.Zero)
            {
                success = WindowsBluetooth.DeviceIoControl(hRadio, WindowsBluetooth.IOCTL_BTH_DISCONNECT_DEVICE, ref addr, 8, IntPtr.Zero, 0, ref bytesReturned, IntPtr.Zero);
                WindowsBluetooth.CloseHandle(hRadio);
                if (!success && !WindowsBluetooth.BluetoothFindNextRadio(search, ref hRadio))
                {
                    hRadio = IntPtr.Zero;
                }
            }

            WindowsBluetooth.BluetoothFindRadioClose(search);
            if (success)
            {
                Logger.Info($"Disconnected {mac}");
            }
            else
            {
                Logger.Error($"Failed to disconnect {mac}");
            }
            return success;
        }
        catch (Exception ex)
        {
            Logger.Error("Error while disconnecting");
            Logger.LogExceptionDetails(ex);
            return false;
        }
    }

    private static string FormatMac(string mac) => string.Join(":", Enumerable.Range(0, 6).Select(i => mac.Substring(i * 2, 2).ToUpper()));

    private static string FormatMacFromBytes(byte[] bytes) => $"{bytes[5]:X2}:{bytes[4]:X2}:{bytes[3]:X2}:{bytes[2]:X2}:{bytes[1]:X2}:{bytes[0]:X2}";
}