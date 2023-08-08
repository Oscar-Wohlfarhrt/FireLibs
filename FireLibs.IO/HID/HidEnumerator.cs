using System.Globalization;
using System.Management;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using static FireLibs.IO.NativeMethods;

namespace FireLibs.IO.HID
{
    /// <summary>
    /// A Hid Device Enumerator class to get available/connected hid devices. Only works on Windows
    /// </summary>
    public static class HidEnumerator
    {
#pragma warning disable CA1416 // Validar la compatibilidad de la plataforma
        /// <summary>
        /// Hid Enumerator using WMI queries to get device path, name and vendor and product ids as HidInfos
        /// </summary>
        /// <param name="vid">Vendor id to filter devices (use 0 to ignore it)</param>
        /// <param name="pid">Product id to filter devices (use 0 to ignore it)</param>
        /// <returns>A HidInfo array</returns>
        public static IEnumerable<HidInfo> WmiEnumerateDevices(int vid = 0, int pid = 0)
        {
            List<HidInfo> infos = new();

            string wmiQuery = @$"SELECT DeviceID FROM Win32_PnPEntity WHERE DeviceID like 'HID%VID%{(vid > 0 ? $"{vid:X4}" : "")}_PID_{(pid > 0 ? $"{pid:X4}" : "")}%'";

            ManagementObjectSearcher searcher = new(wmiQuery);
            ManagementObjectCollection objCollection = searcher.Get();

            foreach (var obj in objCollection)
            {
                string did = obj["DeviceID"].ToString() ?? "";
                Match match = Regex.Match(did, "vid.{1,5}(.{4}).pid.(.{4})", RegexOptions.IgnoreCase);

                if (!int.TryParse(match.Groups[1].Value, NumberStyles.HexNumber, null, out vid))
                    vid = 0;
                if (!int.TryParse(match.Groups[2].Value, NumberStyles.HexNumber, null, out pid))
                    pid = 0;

                infos.Add(new($"\\\\?\\{did.Replace("\\", "#")}#{{4d1e55b2-f16f-11cf-88cb-001111000030}}", "DualShock 4", "", vid, pid));
            }
            return infos;
        }
#pragma warning restore CA1416 // Validar la compatibilidad de la plataforma

        /*
         * Modified by: Oscar-Wohlfarhrt - Github: https://github.com/Oscar-Wohlfarhrt
         * Original from: jhebb and JoshWobbles - Github: https://github.com/InputMapper/Dualshock4
        */
        private static Guid _hidClassGuid = Guid.Empty;
        private static Guid HidClassGuid
        {
            get
            {
                if (_hidClassGuid.Equals(Guid.Empty)) HidD_GetHidGuid(ref _hidClassGuid);
                return _hidClassGuid;
            }
        }
        /// <summary>
        /// Hid Enumerator using SetupApi to get device path, name and vendor and product ids as HidInfos
        /// </summary>
        /// <param name="vendorId">Vendor id to filter devices</param>
        /// <param name="productIds">An array or multiple parameters with product ids to filter devices</param>
        /// <returns>A HidInfo array</returns>
        public static IEnumerable<HidDevice> SetupApiEnumerate(int vendorId, params int[] productIds)
        {
            return EnumerateDevices().Select(x => new HidDevice(x))
                .Where(x => x.Attributes.VendorId == vendorId && productIds.Contains(x.Attributes.ProductId));
        }
        /// <summary>
        /// Hid Enumerator using SetupApi to get device path, name and vendor and product ids as HidInfos
        /// </summary>
        /// <param name="vendorId">Vendor id to filter devices</param>
        /// <returns>A HidInfo array</returns>
        public static IEnumerable<HidDevice> SetupApiEnumerate(int vendorId)
        {
            return EnumerateDevices().Select(x => new HidDevice(x.Path, x.Name))
                .Where(x => x.Attributes.VendorId == vendorId);
        }

        private static IEnumerable<HidInfo> EnumerateDevices()
        {
            List<HidInfo> devices = new();

            Guid HidClass = HidClassGuid;

            IntPtr infoSet = SetupDiGetClassDevs(ref HidClass, null, 0, DIGCF_PRESENT | DIGCF_DEVICEINTERFACE);

            if (infoSet.ToInt64() != INVALID_HANDLE_VALUE)
            {

                SP_DEVINFO_DATA devInfoData = new();
                devInfoData.cbSize = Marshal.SizeOf(devInfoData);

                int deviceIndex = 0;
                while (SetupDiEnumDeviceInfo(infoSet, deviceIndex++, ref devInfoData))
                {
                    SP_DEVICE_INTERFACE_DATA interfaceData = new();
                    interfaceData.cbSize = Marshal.SizeOf(interfaceData);

                    int interfaceIndex = 0;

                    while (SetupDiEnumDeviceInterfaces(infoSet, ref devInfoData, ref HidClass, interfaceIndex, ref interfaceData))
                    {
                        interfaceIndex++;

                        string path = GetDevicePath(infoSet, ref interfaceData);
                        string description = GetBusReportedDeviceDescription(infoSet, ref devInfoData) ?? GetDeviceDescription(infoSet, ref devInfoData);

                        devices.Add(new HidInfo(path, description));
                    }
                }
                _ = SetupDiDestroyDeviceInfoList(infoSet);
            }
            return devices;
        }

        private static string GetDevicePath(IntPtr info, ref SP_DEVICE_INTERFACE_DATA interfaceData)
        {
            int bufferSize = 0;
            SP_DEVICE_INTERFACE_DETAIL_DATA detailInterfaceData = new()
            {
                cbSize = IntPtr.Size == 4 ? 4 + Marshal.SystemDefaultCharSize : 8
            };

            SetupDiGetDeviceInterfaceDetailBuffer(info, ref interfaceData, IntPtr.Zero, 0, ref bufferSize, IntPtr.Zero);
            if (SetupDiGetDeviceInterfaceDetail(info, ref interfaceData, ref detailInterfaceData, bufferSize, ref bufferSize, IntPtr.Zero))
                return detailInterfaceData.DevicePath;

            return "";
        }

        private static string? GetBusReportedDeviceDescription(IntPtr info, ref SP_DEVINFO_DATA devInfoData)
        {
            if (Environment.OSVersion.Version.Major > 5)
            {
                byte[] buffer = new byte[1024];
                ulong type = 0;
                int requiredSize = 0;

                if (SetupDiGetDeviceProperty(info, ref devInfoData, ref DEVPKEY_Device_BusReportedDeviceDesc, ref type, buffer, 1024, ref requiredSize, 0))
                    return buffer.ToUnicodeString();
            }

            return null;
        }
        private static string GetDeviceDescription(IntPtr info, ref SP_DEVINFO_DATA devInfoData)
        {
            byte[] buffer = new byte[1024];
            int type = 0;
            int requiredSize = 0;

            SetupDiGetDeviceRegistryProperty(info, ref devInfoData, SPDRP_DEVICEDESC, ref type, buffer, 1024, ref requiredSize);

            return buffer.ToUTF8String();
        }

        internal static string ToUTF8String(this byte[] buffer)
        {
            var value = Encoding.UTF8.GetString(buffer);
            return value.Remove(value.IndexOf((char)0));
        }

        internal static string ToUnicodeString(this byte[] buffer)
        {
            var value = Encoding.Unicode.GetString(buffer);
            return value.Remove(value.IndexOf((char)0));
        }
    }
}
