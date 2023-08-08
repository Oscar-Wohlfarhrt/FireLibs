using System.Management;

namespace FireLibs.IO.COMPorts
{
    /// <summary>
    /// COM port scanner used to find available/connected ports
    /// </summary>
    public static class COMDeviceScanner
    {
        /// <summary>
        /// Macro to System.IO.Ports.SerialPort.GetPortNames().
        /// Can have errors with duplicated COM ports entries, if a device is unpluged without disconect.
        /// </summary>
        /// <returns>A string array with the port names</returns>
        public static string[] GetPortNames() => System.IO.Ports.SerialPort.GetPortNames();

#pragma warning disable CA1416 // Validar la compatibilidad de la plataforma
#if _WINDOWS
        /// <summary>
        /// A replacement for .NET 'GetPortNames' function only Windows. Fixes the duplicate port entries of the 'GetPortNames' function.
        /// Uses WMI querries to get port name and device name as a structure for SerialPort classes provided in this library.
        /// </summary>
        /// <returns>A SerialDeviceInfo array with the port and device names</returns>
        public static SerialDeviceInfo[] GetSerialDevices()
        {
            List<SerialDeviceInfo> infos = new();

            string wmiQuery = @"SELECT DeviceID, Name FROM Win32_SerialPort";

            ManagementObjectSearcher searcher = new(wmiQuery);
            ManagementObjectCollection objCollection = searcher.Get();


            foreach (ManagementObject obj in objCollection)
            {
                string? port = obj["DeviceID"].ToString();
                string? name = obj["Name"].ToString();

                if (port != null)
                    infos.Add(new(port, (name != null ? name : port)));
            }

            return infos.ToArray();
        }
#else
        /// <summary>
        /// Is a implementation of GetSerialDevices for other OS than Windows.
        /// Can be updated to a native implementation in the future.
        /// </summary>
        /// <returns>A SerialDeviceInfo array with the port names</returns>
        public static SerialDeviceInfo[] GetSerialDevices() => GetPortNames()
                .Select((p) => new SerialDeviceInfo(p, "Unknown")).ToArray();
#endif
#pragma warning restore CA1416 // Validar la compatibilidad de la plataforma
    }
    /// <summary>
    /// Device info structure for COM ports
    /// </summary>
    public struct SerialDeviceInfo
    {
        /// <summary>
        /// Port name stored in the structure
        /// </summary>
        public string PortName { get; private set; }
        /// <summary>
        /// Friendly device name stored in the structure
        /// </summary>
        public string DeviceName { get; private set; }

        /// <summary>
        /// SerialDeviceInfo constructor.
        /// </summary>
        /// <param name="portName">Port Name for the SerialDeviceInfo</param>
        public SerialDeviceInfo(string portName) : this(portName, portName) { }
        /// <summary>
        /// SerialDeviceInfo constructor
        /// </summary>
        /// <param name="portName">Port Name for the SerialDeviceInfo</param>
        /// <param name="devName">Friendly device name for the SerialDeviceInfo</param>
        public SerialDeviceInfo(string portName, string devName)
        {
            PortName = portName;
            DeviceName = devName;
        }
        /// <summary>
        /// Gets a .NET SerialPort class.
        /// </summary>
        /// <param name="baudRate"></param>
        /// <returns>A initialized FireLibs.IO.COMPorts.SerialPort class</returns>
        public SerialPort GetDotNetSerialPort(int baudRate=9600) => new(this, baudRate);
        /// <summary>
        /// Gets a Native SerialPort class.
        /// </summary>
        /// <param name="baudRate"></param>
        /// <returns>A initialized FireLibs.IO.COMPorts.Win.SerialPort class</returns>
        public Win.SerialPort GetWinSerialPort(Win.BaudRates baudRate = Win.BaudRates.BR9600) => new(this, baudRate);
        /// <summary>
        /// Gets the Device and Port names of this info structure
        /// </summary>
        /// <returns>Device and Port names</returns>
        public override string ToString() => $"Name: {DeviceName} [Port: {PortName}]";
    }
}
