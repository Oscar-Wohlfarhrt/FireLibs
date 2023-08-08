using System.Diagnostics;
using System.Management;

namespace FireLibs.IO.COMPorts
{
    /// <summary>
    /// A work arround using the standard System.IO.Ports.SerialPort class.
    /// </summary>
    public class SerialPort
    {
        private readonly System.IO.Ports.SerialPort port = new();

        /// <summary>
        /// Port name of the current SerialPort instance.
        /// </summary>
        public string PortName { get => port.PortName; }
        /// <summary>
        /// Baud rate of the port.
        /// </summary>
        public int BaudRate { get => port.BaudRate; set => port.BaudRate = value; }
        /// <summary>
        /// Name of the device connected to the port.
        /// </summary>
        public string DeviceName { get; private set; }
        /// <summary>
        /// Gets if the COM device is connected
        /// </summary>
        public bool IsConnected { get; private set; }
        /// <summary>
        /// Total timeout for port read operations
        /// </summary>
        public int ReadTimeout { get => port.ReadTimeout; set => port.ReadTimeout = value; }
        /// <summary>
        /// Total timeout for port write operations
        /// </summary>
        public int WriteTimeout { get => port.WriteTimeout; set => port.WriteTimeout = value; }

        /// <summary>
        /// Gets the count of bytes stored in the receive buffer
        /// </summary>
        public int BytesToRead => port.BytesToRead;
        /// <summary>
        /// Gets the count of bytes stored in the send buffer
        /// </summary>
        public int BytesToWrite => port.BytesToWrite;

        /// <summary>
        /// Gets the System.IO.Ports.SerialPort class used by this class. Use to set non-binded properties or functions.
        /// </summary>
        public System.IO.Ports.SerialPort SystemPort => port;
        /// <summary>
        /// Gets the port stream.
        /// </summary>
        public Stream BaseStream => port.BaseStream;
        /// <summary>
        /// SerialPort class constructor.
        /// </summary>
        /// <param name="devInfo">The COM device info struct provided for 'GetSerialDevices' function</param>
        /// <param name="baudRate">The baud rate used for the communication</param>
        public SerialPort(SerialDeviceInfo devInfo, int baudRate = 9600) : this(devInfo.PortName, baudRate, devInfo.DeviceName) { }
        /// <summary>
        /// SerialPort class constructor.
        /// </summary>
        /// <param name="portName">The COM portname</param>
        /// <param name="baudRate">The baud rate used for the communication</param>
        /// <param name="deviceName">The friendly name of the device connected to the port (Optional)</param>
        public SerialPort(string portName, int baudRate = 9600, string deviceName = "Unknown")
        {
            port = new(portName, baudRate);
            DeviceName = deviceName;
        }
        /// <summary>
        /// Connects the port and set it up to use.
        /// </summary>
        /// <returns>The value of 'IsConnected' property. If is true the device has been successfully connected and setup.</returns>
        public bool Connect()
        {
            try
            {
                port.Open();
            }
            catch
            {
                return false;
            }
            return IsConnected = true;
        }
        /// <summary>
        /// Close the port. Use 'Connect' function to reconnect the port.
        /// </summary>
        /// <returns>Returns true the device has been successfully disconnected.</returns>
        public bool Disconnect()
        {
            try
            {
                port.Close();
                IsConnected = false;
            }
            catch
            {
                return false;
            }
            return true;
        }
        /// <summary>
        /// Read a single byte from the recive buffer
        /// </summary>
        /// <returns>The readed byte value</returns>
        public int ReadByte() => port.ReadByte();
        /// <summary>
        /// Reads all the bytes in the recive buffer as a string
        /// </summary>
        /// <returns>A ASCII string of the bytes readed</returns>
        public string ReadExisting() => port.ReadExisting();
        /// <summary>
        /// Reads a char array from the recived buffer.
        /// </summary>
        /// <param name="buffer">Buffer array to be readed</param>
        /// <param name="offset">Offset from the start of the array</param>
        /// <param name="count">The count of bytes to read</param>
        /// <returns></returns>
        public int Read(char[] buffer, int offset, int count) => port.Read(buffer, offset, count);
        /// <summary>
        /// Reads a byte array from the recived buffer.
        /// </summary>
        /// <param name="buffer">Buffer array to be readed</param>
        /// <param name="offset">Offset from the start of the array</param>
        /// <param name="count">The count of bytes to read</param>
        /// <returns></returns>
        public int Read(byte[] buffer, int offset, int count) => port.Read(buffer, offset, count);
        /// <summary>
        /// Reads a byte array from the recived buffer if all the requested bytes are recived. If the requested bytes are not available in the ReadTimeout time, nothing is readed.
        /// </summary>
        /// <param name="buffer">Buffer array to be readed</param>
        /// <param name="offset">Offset from the start of the array</param>
        /// <param name="count">The count of bytes to read</param>
        /// <returns>If the read was successful</returns>
        public bool ReadCount(byte[] buffer,int offset,int count)
        {
            Stopwatch watch = new();
            watch.Restart();

            while (BytesToRead < count && watch.ElapsedMilliseconds < ReadTimeout) ;
            watch.Stop();

            if (BytesToRead >= count)
            {
                port.Read(buffer, offset, count);
                return true;
            }
            return false;
        }
        /// <summary>
        /// Writes a string into the port.
        /// </summary>
        /// <param name="text">String to be written to the port</param>
        public void Write(string text) => port.Write(text);
        /// <summary>
        /// Writes a string into the port and a end of line character.
        /// </summary>
        /// <param name="text">String to be written to the port</param>
        public void WriteLine(string text) => port.WriteLine(text);
        /// <summary>
        /// Writes a char array into the port.
        /// </summary>
        /// <param name="buffer">Buffer array to be written</param>
        /// <param name="offset">Offset from the start of the array</param>
        /// <param name="count">The count of bytes to read</param>
        public void Write(char[] buffer, int offset, int count) => port.Write(buffer, offset, count);
        /// <summary>
        /// Writes a byte array into the port.
        /// </summary>
        /// <param name="buffer">Buffer array to be written</param>
        /// <param name="offset">Offset from the start of the array</param>
        /// <param name="count">The count of bytes to read</param>
        public void Write(byte[] buffer, int offset, int count) => port.Write(buffer, offset, count);
        /// <summary>
        /// Infinite timeout constant of System.IO.Ports.SerialPort
        /// </summary>
        public const int InfinityTimeout = System.IO.Ports.SerialPort.InfiniteTimeout;
        /// <summary>
        /// Macro to System.IO.Ports.SerialPort.GetPortNames().
        /// Can have errors with duplicated COM ports entries, if a device is unpluged without disconect.
        /// </summary>
        /// <returns>A string array with the port names</returns>
        public static string[] GetPortNames() => COMDeviceScanner.GetPortNames();
        /// <summary>
        /// A replacement for .NET 'GetPortNames' function. Fixes the duplicate port entries of the 'GetPortNames' function on windows.
        /// In windowsUses WMI queries to get port name and device name as a structure for SerialPort classes provided in this library.
        /// For other OS uses GetPortNames but returning a SerialDeviceInfo array (Can be updated to a native implementation in the future). 
        /// </summary>
        /// <returns>A SerialDeviceInfo array with the port and device names</returns>
        public static SerialDeviceInfo[] GetSerialDevices()=>COMDeviceScanner.GetSerialDevices();
    }
}