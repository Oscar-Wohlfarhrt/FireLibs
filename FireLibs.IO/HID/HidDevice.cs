using Microsoft.Win32.SafeHandles;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using static FireLibs.IO.HID.NativeMethods;

namespace FireLibs.IO.HID
{
    public class HidDevice
    {
        #region DSHidDevice

        DeviceInfo deviceInfo;
        HidDeviceAttributes deviceAttributes;
        HidDeviceCapabilities deviceCapabilities;
        SafeFileHandle? safeFileHandle;
        string? serial;
        private const bool defaultExclusiveMode = false;

        public bool IsExclusive { get; private set; }
        public bool IsOpen { get; private set; }
        public FileStream? FileStream { get; private set; }
        public DeviceInfo Information { get { return deviceInfo; } }
        public HidDeviceAttributes Attributes { get { return deviceAttributes; } }
        public HidDeviceCapabilities Capabilities { get { return deviceCapabilities; } }

#pragma warning disable CS8618
        public HidDevice(string path, string description = "")
        {
            deviceInfo = new DeviceInfo(path, description);
            LoadAtributes();
        }
        public HidDevice(DeviceInfo info)
        {
            deviceInfo = info;
            LoadAtributes();
        }
#pragma warning restore CS8618

        private void LoadAtributes()
        {
            try
            {
                var hidHandle = OpenHandle(deviceInfo.Path, false);

                deviceAttributes = GetDeviceAttributes(hidHandle);
                deviceCapabilities = GetDeviceCapabilities(hidHandle);

                hidHandle.Close();
            }
            catch (Exception exception)
            {
                System.Diagnostics.Trace.TraceError(exception.Message);
                throw new Exception(string.Format("Error querying HID device '{0}'.", deviceInfo.Path), exception);

            }
        }
        private static HidDeviceAttributes GetDeviceAttributes(SafeFileHandle hidHandle)
        {
            var deviceAttributes = default(HIDD_ATTRIBUTES);
            deviceAttributes.Size = Marshal.SizeOf(deviceAttributes);
            HidD_GetAttributes(hidHandle.DangerousGetHandle(), ref deviceAttributes);
            return new HidDeviceAttributes(deviceAttributes);
        }
        private static HidDeviceCapabilities GetDeviceCapabilities(SafeFileHandle hidHandle)
        {
            var capabilities = default(HIDP_CAPS);
            var preparsedDataPointer = default(IntPtr);

            if (HidD_GetPreparsedData(hidHandle.DangerousGetHandle(), ref preparsedDataPointer))
            {
                HidP_GetCaps(preparsedDataPointer, ref capabilities);
                HidD_FreePreparsedData(preparsedDataPointer);
            }
            return new HidDeviceCapabilities(capabilities);
        }
        private SafeFileHandle OpenHandle(string devicePathName, bool isExclusive = false)
        {
            SafeFileHandle hidHandle;

            try
            {
                if (isExclusive)
                {
                    hidHandle = CreateFile(devicePathName, GENERIC_READ | GENERIC_WRITE, 0, IntPtr.Zero, OpenExisting, 0, 0);
                }
                else
                {
                    hidHandle = CreateFile(devicePathName, GENERIC_READ | GENERIC_WRITE, FILE_SHARE_READ | FILE_SHARE_WRITE, IntPtr.Zero, OpenExisting, 0, 0);
                }
            }
            catch (Exception)
            {
                throw;
            }
            return hidHandle;
        }

        public void OpenDevice(bool isExclusive)
        {
            if (IsOpen) return;
            try
            {
                if (safeFileHandle == null || safeFileHandle.IsInvalid)
                    safeFileHandle = OpenHandle(deviceInfo.Path, isExclusive);
            }
            catch (Exception exception)
            {
                IsOpen = false;
                throw new Exception("Error opening HID device.", exception);
            }

            IsOpen = !safeFileHandle.IsInvalid;
            IsExclusive = isExclusive;
        }

        public void CloseDevice()
        {
            if (!IsOpen) return;
            closeFileStreamIO();

            IsOpen = false;
        }
        private void closeFileStreamIO()
        {
            if (FileStream != null)
                FileStream.Close();
            FileStream = null;
            if (safeFileHandle != null && !safeFileHandle.IsInvalid)
            {
                safeFileHandle.Close();
            }
            safeFileHandle = null;
        }
        public void Dispose()
        {
            CancelIO();
            CloseDevice();
        }
        public void CancelIO()
        {
            if (IsOpen && safeFileHandle != null)
                CancelIoEx(safeFileHandle.DangerousGetHandle(), IntPtr.Zero);
        }
        public bool ReadInputReport(byte[] data)
        {
            if (safeFileHandle == null)
                safeFileHandle = OpenHandle(deviceInfo.Path, defaultExclusiveMode);
            return HidD_GetInputReport(safeFileHandle, data, data.Length);
        }
        public ReadStatus ReadFile(byte[] inputBuffer)
        {
            if (safeFileHandle == null)
                safeFileHandle = OpenHandle(deviceInfo.Path, defaultExclusiveMode);
            try
            {
                if (NativeMethods.ReadFile(safeFileHandle.DangerousGetHandle(), inputBuffer, (uint)inputBuffer.Length, out uint bytesRead, IntPtr.Zero))
                {
                    return ReadStatus.Success;
                }
                else
                {
                    return ReadStatus.NoDataRead;
                }
            }
            catch (Exception)
            {
                return ReadStatus.ReadError;
            }
        }
        public ReadStatus WriteFile(byte[] inputBuffer)
        {
            if (safeFileHandle == null)
                safeFileHandle = OpenHandle(deviceInfo.Path, defaultExclusiveMode);
            try
            {
                if (NativeMethods.WriteFile(safeFileHandle.DangerousGetHandle(), inputBuffer, (uint)inputBuffer.Length, out uint bytesWrite, IntPtr.Zero))
                {
                    return ReadStatus.Success;
                }
                else
                {
                    return ReadStatus.NoDataRead;
                }
            }
            catch (Exception)
            {
                return ReadStatus.ReadError;
            }
        }

        public bool WriteOutputReportViaControl(byte[] outputBuffer)
        {
            try
            {
                if (safeFileHandle == null)
                    safeFileHandle = OpenHandle(deviceInfo.Path, defaultExclusiveMode);
                return HidD_SetOutputReport(safeFileHandle, outputBuffer, outputBuffer.Length);
            }
            catch
            {
                return false;
            }
        }
        public bool GetFeature(byte[] data)
        {
            if (safeFileHandle == null)
                safeFileHandle = OpenHandle(deviceInfo.Path, defaultExclusiveMode);
            return HidD_GetFeature(safeFileHandle.DangerousGetHandle(), data, data.Length);
        }
        public bool SetFeature(byte[] data)
        {
            if (safeFileHandle == null)
                safeFileHandle = OpenHandle(deviceInfo.Path, defaultExclusiveMode);
            return HidD_SetFeature(safeFileHandle.DangerousGetHandle(), data, data.Length);
        }
        public string readSerial()
        {
            if (serial != null)
                return serial;

            try
            {
                if (!IsOpen || safeFileHandle == null)
                    safeFileHandle = OpenHandle(deviceInfo.Path, defaultExclusiveMode);

                if (Capabilities.InputReportByteLength == 64)
                {
                    byte[] buffer = new byte[16];
                    buffer[0] = 18;
                    GetFeature(buffer);
                    serial = string.Format("{0:X02}:{1:X02}:{2:X02}:{3:X02}:{4:X02}:{5:X02}", buffer[6], buffer[5], buffer[4], buffer[3], buffer[2], buffer[1]);
                    return serial;

                }
                else
                {
                    byte[] buffer = new byte[126];
                    if (HidD_GetSerialNumberString(safeFileHandle.DangerousGetHandle(), buffer, (uint)buffer.Length))
                    {
                        string MACAddr = Encoding.Unicode.GetString(buffer).Replace("\0", string.Empty).ToUpper();
                        return serial = string.Format("{0}{1}:{2}{3}:{4}{5}:{6}{7}:{8}{9}:{10}{11}",
                            MACAddr[0], MACAddr[1], MACAddr[2], MACAddr[3], MACAddr[4],
                            MACAddr[5], MACAddr[6], MACAddr[7], MACAddr[8],
                            MACAddr[9], MACAddr[10], MACAddr[11]);
                    }
                    else
                    {
                        return serial = GenerateFakeMAC();
                    }
                }
            }
            catch (Exception err)
            {
                if (err.GetType() == typeof(IndexOutOfRangeException))
                    return serial = GenerateFakeMAC();
            }

            if (serial == null)
                return "";

            return serial;
        }
        private string GenerateFakeMAC()
        {
            string FakeMAC;
            using (MD5 md5 = MD5.Create())
            {
                FakeMAC = BitConverter.ToString(
                  md5.ComputeHash(Encoding.UTF8.GetBytes(deviceInfo.Path))
                ).Replace("-", ":");
            }
            return $"99:{FakeMAC[..14]}";
        }
        #endregion

        public enum ReadStatus
        {
            Success = 0,
            WaitTimedOut = 1,
            WaitFail = 2,
            NoDataRead = 3,
            ReadError = 4,
            NotConnected = 5
        }
        public class DeviceInfo
        {
            public string Path { get; private set; }
            public string Description { get; private set; }

            public DeviceInfo(string path, string description)
            {
                Path = path;
                Description = description;
            }
        }
        public class HidDeviceAttributes
        {
            internal HidDeviceAttributes(HIDD_ATTRIBUTES attributes)
            {
                VendorId = attributes.VendorID;
                ProductId = attributes.ProductID;
                Version = attributes.VersionNumber;

                VendorHexId = "0x" + attributes.VendorID.ToString("X4");
                ProductHexId = "0x" + attributes.ProductID.ToString("X4");
            }

            public int VendorId { get; private set; }
            public int ProductId { get; private set; }
            public int Version { get; private set; }
            public string VendorHexId { get; set; }
            public string ProductHexId { get; set; }
        }
        public class HidDeviceCapabilities
        {
            internal HidDeviceCapabilities(HIDP_CAPS capabilities)
            {
                Usage = capabilities.Usage;
                UsagePage = capabilities.UsagePage;
                InputReportByteLength = capabilities.InputReportByteLength;
                OutputReportByteLength = capabilities.OutputReportByteLength;
                FeatureReportByteLength = capabilities.FeatureReportByteLength;
                Reserved = capabilities.Reserved;
                NumberLinkCollectionNodes = capabilities.NumberLinkCollectionNodes;
                NumberInputButtonCaps = capabilities.NumberInputButtonCaps;
                NumberInputValueCaps = capabilities.NumberInputValueCaps;
                NumberInputDataIndices = capabilities.NumberInputDataIndices;
                NumberOutputButtonCaps = capabilities.NumberOutputButtonCaps;
                NumberOutputValueCaps = capabilities.NumberOutputValueCaps;
                NumberOutputDataIndices = capabilities.NumberOutputDataIndices;
                NumberFeatureButtonCaps = capabilities.NumberFeatureButtonCaps;
                NumberFeatureValueCaps = capabilities.NumberFeatureValueCaps;
                NumberFeatureDataIndices = capabilities.NumberFeatureDataIndices;

            }

            public short Usage { get; private set; }
            public short UsagePage { get; private set; }
            public short InputReportByteLength { get; private set; }
            public short OutputReportByteLength { get; private set; }
            public short FeatureReportByteLength { get; private set; }
            public short[] Reserved { get; private set; }
            public short NumberLinkCollectionNodes { get; private set; }
            public short NumberInputButtonCaps { get; private set; }
            public short NumberInputValueCaps { get; private set; }
            public short NumberInputDataIndices { get; private set; }
            public short NumberOutputButtonCaps { get; private set; }
            public short NumberOutputValueCaps { get; private set; }
            public short NumberOutputDataIndices { get; private set; }
            public short NumberFeatureButtonCaps { get; private set; }
            public short NumberFeatureValueCaps { get; private set; }
            public short NumberFeatureDataIndices { get; private set; }
        }

        #region Static Methods
        private static Guid _hidClassGuid = Guid.Empty;
        public static Guid HidClassGuid
        {
            get
            {
                if (_hidClassGuid.Equals(Guid.Empty)) HidD_GetHidGuid(ref _hidClassGuid);
                return _hidClassGuid;
            }
        }

        public static IEnumerable<HidDevice> Enumerate(int vendorId, params int[] productIds)
        {
            return EnumerateDevices().Select(x => new HidDevice(x))
                .Where(x => x.Attributes.VendorId == vendorId && productIds.Contains(x.Attributes.ProductId));
        }
        public static IEnumerable<HidDevice> Enumerate(int vendorId)
        {
            return EnumerateDevices().Select(x => new HidDevice(x.Path, x.Description))
                .Where(x => x.Attributes.VendorId == vendorId);
        }

        private static IEnumerable<DeviceInfo> EnumerateDevices()
        {
            List<DeviceInfo> devices = new List<DeviceInfo>();

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

                        devices.Add(new DeviceInfo(path, description));
                    }
                }
                SetupDiDestroyDeviceInfoList(infoSet);
            }
            return devices;
        }

        private static string GetDevicePath(IntPtr info, ref SP_DEVICE_INTERFACE_DATA interfaceData)
        {
            int bufferSize = 0;
            SP_DEVICE_INTERFACE_DETAIL_DATA detailInterfaceData = new SP_DEVICE_INTERFACE_DETAIL_DATA();
            detailInterfaceData.cbSize = IntPtr.Size == 4 ? 4 + Marshal.SystemDefaultCharSize : 8;

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
                    return ToUnicodeString(buffer);
            }

            return null;
        }
        private static string GetDeviceDescription(IntPtr info, ref SP_DEVINFO_DATA devInfoData)
        {
            byte[] buffer = new byte[1024];
            int type = 0;
            int requiredSize = 0;

            SetupDiGetDeviceRegistryProperty(info, ref devInfoData, SPDRP_DEVICEDESC, ref type, buffer, 1024, ref requiredSize);

            return ToUTF8String(buffer);
        }

        #endregion
        private static string ToUTF8String(byte[] buffer)
        {
            var value = Encoding.UTF8.GetString(buffer);
            return value.Remove(value.IndexOf((char)0));
        }

        private static string ToUnicodeString(byte[] buffer)
        {
            var value = Encoding.Unicode.GetString(buffer);
            return value.Remove(value.IndexOf((char)0));
        }
    }
}
