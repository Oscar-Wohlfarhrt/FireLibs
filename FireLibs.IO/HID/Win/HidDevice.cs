/*
 * Modified by: Oscar-Wohlfarhrt - Github: https://github.com/Oscar-Wohlfarhrt
 * Original from: jhebb and JoshWobbles - Github: https://github.com/InputMapper/Dualshock4
*/

using Microsoft.Win32.SafeHandles;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using static FireLibs.IO.NativeMethods;

namespace FireLibs.IO.HID.Win
{
    /// <summary>
    /// A class for HidDevices. Only works on windows.
    /// </summary>
    public class HidDevice : IDisposable
    {
        private const bool defaultExclusiveMode = false;

        private readonly HidInfo deviceInfo = null!;
        private HidDeviceAttributes deviceAttributes = null!;
        private HidDeviceCapabilities deviceCapabilities = null!;
        private SafeFileHandle? safeFileHandle = null;
        private string? serial = null;

        /// <summary>
        /// Gets if the Hid Device is opened on exclusive mode. If is in exclusive mode no other program can write to the device.
        /// </summary>
        public bool IsExclusive { get; private set; }
        /// <summary>
        /// Gets if the Hid Device is Opened/Connected.
        /// </summary>
        public bool IsOpen { get; private set; }
        /// <summary>
        /// Get the Hid Device information.
        /// </summary>
        public HidInfo Information { get { return deviceInfo; } }
        /// <summary>
        /// Get the Hid Device atributes.
        /// </summary>
        public HidDeviceAttributes Attributes { get { return deviceAttributes; } }
        /// <summary>
        /// Get the Hid Device capabilities.
        /// </summary>
        public HidDeviceCapabilities Capabilities { get { return deviceCapabilities; } }
        /// <summary>
        /// Gets the serial number of the device, if it has one, otherwise it returns a fake one generated using the path property.
        /// If serial number is null anyway, it returns an empty string.
        /// </summary>
        public string Serial { get => serial ?? string.Empty; }
        /// <summary>
        /// HidDevice class constructor.
        /// </summary>
        /// <param name="path">The windows device path</param>
        /// <param name="description">The friendly name of the device</param>
        public HidDevice(string path, string description = "") : this(new(path, description)) { }
        /// <summary>
        /// HidDevice class constructor.
        /// </summary>
        /// <param name="info">HidInfo class containing the device information</param>
        public HidDevice(HidInfo info)
        {
            deviceInfo = info;
            LoadAtributes();
            if (Information.ProductId <= 0)
                Information.ProductId = Attributes.ProductId;
            if (Information.VendorId <= 0)
                Information.VendorId = Attributes.VendorId;

            int trys = 3;
            serial = null;
            while (trys > 0 && (serial = ReadSerial())==null) ;
            Information.Id = serial ?? info.Path;

            serial ??= GenerateFakeMAC();

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
                _ = HidP_GetCaps(preparsedDataPointer, ref capabilities);
                HidD_FreePreparsedData(preparsedDataPointer);
            }
            return new HidDeviceCapabilities(capabilities);
        }
        /* ------- Serial ------- */
        private string? ReadSerial()
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
        private static SafeFileHandle OpenHandle(string devicePathName, bool isExclusive = false)
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
        /// <summary>
        /// Open/Connects the Hid Device.
        /// </summary>
        /// <param name="isExclusive">A tru/false value indicating if the device is opened in exclusive mode</param>
        /// <exception cref="Exception">Thows an exeption if the device fails to open</exception>
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
        /// <summary>
        /// Close/Disconnect the Hid Device.
        /// </summary>
        public void CloseDevice()
        {
            if (!IsOpen) return;
            CloseFileStreamIO();

            IsOpen = false;
        }
        private void CloseFileStreamIO()
        {
            if (safeFileHandle != null && !safeFileHandle.IsInvalid)
            {
                safeFileHandle.Close();
            }
            safeFileHandle = null;
        }
        /// <summary>
        /// Dispose function for HidDevice class. Cancel the current transmisions and close the device.
        /// </summary>
        public void Dispose()
        {
            CancelIO();
            CloseDevice();
            GC.SuppressFinalize(this);
        }
        /// <summary>
        /// Cancel current Input/Output operations of the device.
        /// </summary>
        public void CancelIO()
        {
            if (IsOpen && safeFileHandle != null)
                CancelIoEx(safeFileHandle.DangerousGetHandle(), IntPtr.Zero);
        }
        #endregion Close Device

        #region I/O Operations
        /* ------- Read ------- */
        /// <summary>
        /// Reads the input report from the device
        /// </summary>
        /// <param name="data">A byte array to read the report. First byte is the report id.</param>
        /// <returns>True if the read process is successful</returns>
        public bool ReadInputReport(byte[] data)
        {
            safeFileHandle ??= OpenHandle(deviceInfo.Path, defaultExclusiveMode);
            return HidD_GetInputReport(safeFileHandle, data, data.Length);
        }
        /// <summary>
        /// Reads directly from the Hid Device File/Input buffer using Windows FileApi
        /// </summary>
        /// <param name="inputBuffer">A byte array to read the file data</param>
        /// <returns>A ReadWriteStatus enumeration</returns>
        public ReadWriteStatus ReadFile(byte[] inputBuffer)
        {
            safeFileHandle ??= OpenHandle(deviceInfo.Path, defaultExclusiveMode);
            try
            {
                if (NativeMethods.ReadFile(safeFileHandle.DangerousGetHandle(), inputBuffer, (uint)inputBuffer.Length, out uint bytesRead, IntPtr.Zero))
                {
                    return ReadWriteStatus.Success;
                }
                else
                {
                    return ReadWriteStatus.NoData;
                }
            }
            catch (Exception)
            {
                return ReadWriteStatus.Error;
            }
        }
        /// <summary>
        /// Reads a structure directly from the Hid Device File/Input buffer using Windows FileApi.
        /// </summary>
        /// <typeparam name="T">Type of the structure to be read</typeparam>
        /// <param name="buffer">The readed structure</param>
        /// <returns>A ReadWriteStatus enumeration</returns>
        public ReadWriteStatus ReadFile<T>(out T buffer) where T : struct
        {
            safeFileHandle ??= OpenHandle(deviceInfo.Path, defaultExclusiveMode);
            IntPtr strPtr = Marshal.AllocHGlobal(Marshal.SizeOf<T>());
            buffer = default;
            try
            {
                if (NativeMethods.ReadFile(safeFileHandle.DangerousGetHandle(), strPtr, (uint)Marshal.SizeOf<T>(), out uint bytesRead, IntPtr.Zero))
                {
                    buffer = Marshal.PtrToStructure<T>(strPtr);
                    Marshal.FreeHGlobal(strPtr);
                    return ReadWriteStatus.Success;
                }
                else
                {
                    Marshal.FreeHGlobal(strPtr);
                    return ReadWriteStatus.NoData;
                }
            }
            catch (Exception)
            {
                Marshal.FreeHGlobal(strPtr);
                return ReadWriteStatus.Error;
            }
            finally
            {
                Marshal.FreeHGlobal(strPtr);
            }
        }
        /// <summary>
        /// Unsafe version of ReadFile function.
        /// Reads directly from the Hid Device File/Input buffer using Windows FileApi
        /// </summary>
        /// <param name="buffer">Unsafe pointer to the structure/array</param>
        /// <param name="offset">Offset of the data inside of the pointer</param>
        /// <param name="count">Count of bytes to read</param>
        /// <returns>A ReadWriteStatus enumeration</returns>
        public unsafe ReadWriteStatus ReadFile(int* buffer, uint offset, uint count)
        {
            safeFileHandle ??= OpenHandle(deviceInfo.Path, defaultExclusiveMode);
            buffer = default;
            try
            {
                if (NativeMethods.ReadFile(safeFileHandle.DangerousGetHandle(), buffer + offset, count, out uint bytesRead, IntPtr.Zero))
                {
                    return ReadWriteStatus.Success;
                }
                else
                {
                    return ReadWriteStatus.NoData;
                }
            }
            catch (Exception)
            {
                return ReadWriteStatus.Error;
            }
        }

        /* ------- Write ------- */
        /// <summary>
        /// Writes directly to the Hid Device File/Output buffer using Windows FileApi
        /// </summary>
        /// <param name="outputBuffer">A byte array containing the data to be written</param>
        /// <returns>A ReadWriteStatus enumeration</returns>
        public ReadWriteStatus WriteFile(byte[] outputBuffer)
        {
            safeFileHandle ??= OpenHandle(deviceInfo.Path, defaultExclusiveMode);
            try
            {
                if (NativeMethods.WriteFile(safeFileHandle.DangerousGetHandle(), outputBuffer, (uint)outputBuffer.Length, out uint bytesWrite, IntPtr.Zero))
                {
                    return ReadWriteStatus.Success;
                }
                else
                {
                    return ReadWriteStatus.NoData;
                }
            }
            catch (Exception)
            {
                return ReadWriteStatus.Error;
            }
        }
        /// <summary>
        /// Writes an output report to the device.
        /// </summary>
        /// <param name="outputBuffer">A byte array containing the data to be written. First byte is the report id.</param>
        /// <returns>True if the write process is successful</returns>
        public bool WriteOutputReport(byte[] outputBuffer)
        {
            try
            {
                safeFileHandle ??= OpenHandle(deviceInfo.Path, defaultExclusiveMode);
                return HidD_SetOutputReport(safeFileHandle, outputBuffer, outputBuffer.Length);
            }
            catch
            {
                return false;
            }
        }
        /// <summary>
        /// Writes a structure directly to the Hid Device File/Output buffer using Windows FileApi.
        /// </summary>
        /// <typeparam name="T">Type of the structure to be written</typeparam>
        /// <param name="buffer">The structure to be written</param>
        /// <returns>A ReadWriteStatus enumeration</returns>
        public ReadWriteStatus WriteFile<T>(T buffer) where T : struct
        {
            safeFileHandle ??= OpenHandle(deviceInfo.Path, defaultExclusiveMode);
            IntPtr strPtr = Marshal.AllocHGlobal(Marshal.SizeOf<T>());
            try
            {
                Marshal.StructureToPtr(buffer, strPtr, true);
                if (NativeMethods.WriteFile(safeFileHandle.DangerousGetHandle(), strPtr, (uint)Marshal.SizeOf<T>(), out uint bytesWrite, IntPtr.Zero))
                {
                    Marshal.FreeHGlobal(strPtr);
                    return ReadWriteStatus.Success;
                }
                else
                {
                    Marshal.FreeHGlobal(strPtr);
                    return ReadWriteStatus.NoData;
                }
            }
            catch (Exception)
            {
                Marshal.FreeHGlobal(strPtr);
                return ReadWriteStatus.Error;
            }
        }
        /// <summary>
        /// Unsafe version of WriteFile function.
        /// Writes directly to the Hid Device File/Output buffer using Windows FileApi
        /// </summary>
        /// <param name="buffer">Unsafe pointer to the structure/array</param>
        /// <param name="offset">Offset of the data inside of the pointer</param>
        /// <param name="count">Count of bytes to write</param>
        /// <returns>A ReadWriteStatus enumeration</returns>
        public unsafe ReadWriteStatus WriteFile(int* buffer,uint offset, uint count)
        {
            safeFileHandle ??= OpenHandle(deviceInfo.Path, defaultExclusiveMode);
            try
            {
                if (NativeMethods.WriteFile(safeFileHandle.DangerousGetHandle(), buffer + offset, count, out uint bytesWrite, IntPtr.Zero))
                {
                    return ReadWriteStatus.Success;
                }
                else
                {
                    return ReadWriteStatus.NoData;
                }
            }
            catch (Exception)
            {
                return ReadWriteStatus.Error;
            }
        }

        /* ------- Feature ------- */
        /// <summary>
        /// Gets a feature report from the Hid Device
        /// </summary>
        /// <param name="data">A byte array containing the data to be readed. First byte is the report id.</param>
        /// <returns>True if the read process is successful</returns>
        public bool GetFeature(byte[] data)
        {
            safeFileHandle ??= OpenHandle(deviceInfo.Path, defaultExclusiveMode);
            return HidD_GetFeature(safeFileHandle.DangerousGetHandle(), data, data.Length);
        }
        /// <summary>
        /// Sets a feature report from the Hid Device
        /// </summary>
        /// <param name="data">A byte array containing the data to be written. First byte is the report id.</param>
        /// <returns>True if the write process is successful</returns>
        public bool SetFeature(byte[] data)
        {
            safeFileHandle ??= OpenHandle(deviceInfo.Path, defaultExclusiveMode);
            return HidD_SetFeature(safeFileHandle.DangerousGetHandle(), data, data.Length);
        }
        #endregion I/O Operations

        #region Additional configs
        /// <summary>
        /// Gets the number of buffers assigned to this device
        /// </summary>
        /// <param name="count">The number of input buffers this device have</param>
        /// <returns>True if the operation was successful</returns>
        public bool GetInputBufferCount(out int count)
        {
            safeFileHandle ??= OpenHandle(deviceInfo.Path, defaultExclusiveMode);
            count = 0;
            return HidD_GetNumInputBuffers(safeFileHandle.DangerousGetHandle(), ref count);
        }
        /// <summary>
        /// Sets the number of buffers assigned to this device
        /// </summary>
        /// <param name="count">The number of input buffers to set</param>
        /// <returns>True if the operation was successful</returns>
        public bool SetInputBufferCount(int count)
        {
            safeFileHandle ??= OpenHandle(deviceInfo.Path, defaultExclusiveMode);
            return HidD_SetNumInputBuffers(safeFileHandle.DangerousGetHandle(), count);
        }
        #endregion Additional configs
    }
}