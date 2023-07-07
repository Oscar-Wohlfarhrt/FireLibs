/*
 * Modified by: Oscar-Wohlfarhrt - Github: https://github.com/Oscar-Wohlfarhrt
 * Original from: jhebb and JoshWobbles - Github: https://github.com/InputMapper/Dualshock4
*/

using Microsoft.Win32.SafeHandles;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using static FireLibs.IO.HID.NativeMethods;

namespace FireLibs.IO.HID
{
    public class HidDevice : IDisposable
    {
        private const bool defaultExclusiveMode = false;

        DSHidInfo deviceInfo;
        HidDeviceAttributes deviceAttributes;
        HidDeviceCapabilities deviceCapabilities;
        SafeFileHandle? safeFileHandle;
        string? serial;

        public bool IsExclusive { get; private set; }
        public bool IsOpen { get; private set; }
        public FileStream? FileStream { get; private set; }
        public DSHidInfo Information { get { return deviceInfo; } }
        public HidDeviceAttributes Attributes { get { return deviceAttributes; } }
        public HidDeviceCapabilities Capabilities { get { return deviceCapabilities; } }

        public HidDevice(string path, string description = "") : this(new(path, description)) { }
        public HidDevice(DSHidInfo info)
        {
            deviceInfo = info;
            LoadAtributes();
            if (Information.ProductId <= 0)
                Information.ProductId = Attributes.ProductId;
            if (Information.VendorId <= 0)
                Information.VendorId = Attributes.VendorId;

            int trys = 3;
            string? serial = null;
            while (trys > 0 && (serial = ReadSerial())==null) ;
            Information.Id = serial ?? info.Path;

            if (serial == null)
                serial = GenerateFakeMAC();

            CancelIO();
            CloseDevice();
        }

        #region Device Atributes
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
                throw new Exception($"Error querying HID device '{deviceInfo.Path}'.", exception);
            }
        }

        /* ------- Common ------- */
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
        /* ------- Serial ------- */
        public string? ReadSerial()
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
                }
            }
            catch { }

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
        #endregion Device Atributes

        #region Open Device
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
        #endregion Open Device

        #region Close Device
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
        #endregion Close Device

        #region I/O Operations
        /* ------- Read ------- */
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

        /* ------- Write ------- */
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

        /* ------- Feature ------- */
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
        #endregion I/O Operations
    }
}