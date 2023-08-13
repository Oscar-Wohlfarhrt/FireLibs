using Microsoft.Win32.SafeHandles;
using System.Runtime.InteropServices;
using static FireLibs.IO.NativeMethods;

namespace FireLibs.IO.COMPorts.Win
{
    /// <summary>
    /// Implementation of SerialPort class using Fileapi and WinBase native Windows libraries
    /// </summary>
    public class SerialPort
    {
        private SafeFileHandle? port;
        private DCB dcb;
        private COMMTIMEOUTS timeouts;

        /// <summary>
        /// Name of the device connected to the port.
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// Port name of the current SerialPort instance.
        /// </summary>
        public string PortName { get; private set; }
        /// <summary>
        /// Baud rate of the port. Aplied when port is connected or with 'UpdateConfig'.
        /// </summary>
        public BaudRates BaudRate { get; set; }
        /// <summary>
        /// The parity-checking protocol used in the port. Aplied when port is connected or with 'UpdateConfig'.
        /// </summary>
        public Parity Parity { get; set; } = Parity.None;
        /// <summary>
        /// The number of stop bits used in the port. Aplied when port is connected or with 'UpdateConfig'.
        /// </summary>
        public StopBits StopBits { get; set; } = StopBits.One;
        /// <summary>
        /// The number of bits of a byte (value range: 4-8). Aplied when port is connected or with 'UpdateConfig'.
        /// </summary>
        public byte ByteSize { get; set; } = 8;
        /// <summary>
        /// The Request to Send configuration for the port. Aplied when port is connected or with 'UpdateConfig'.
        /// </summary>
        public RtsControl RtsControl { get; set; } = RtsControl.Disable;
        /// <summary>
        /// The Data Terminal Ready configuration for the port. Aplied when port is connected or with 'UpdateConfig'.
        /// </summary>
        public DtrControl DtrControl { get; set; } = DtrControl.Disable;
        /// <summary>
        /// Gets if the COM device is connected
        /// </summary>
        public bool IsConnected { get; private set; } = false;

        /// <summary>
        /// Read timeout between two bytes. If it is exceeded, the operation will terminate, even if not all bytes have been read. Use 0 for an infinite timeout.
        /// To read only the bytes already in the buffer and return immediately, even if there are no bytes in the buffer,use uint.MaxValue in this property and a 0 value in both 'ReadTotalTimeoutConstant' and 'ReadTotalTimeoutMultiplier'.
        /// </summary>
        public uint ReadIntervalTimeout { get => timeouts.ReadIntervalTimeout; set => timeouts.ReadIntervalTimeout = value; }
        /// <summary>
        /// Read timeout per byte. Used to calculate total timeout multiplying it by the number of bytes to read and 'ReadTotalTimeoutMultiplier'. Use 0 in this and 'ReadTotalTimeoutMultiplier' for an infinite timeout.
        /// </summary>
        public uint ReadTotalTimeoutConstant { get => timeouts.ReadTotalTimeoutConstant; set=> timeouts.ReadTotalTimeoutConstant = value; }
        /// <summary>
        /// Read timeout multiplier. Used to calculate total timeout multiplying it by the number of bytes to read and 'ReadTotalTimeoutConstant'. Use 0 in this and 'ReadTotalTimeoutConstant' for an infinite timeout.
        /// </summary>
        public uint ReadTotalTimeoutMultiplier { get => timeouts.ReadTotalTimeoutMultiplier; set => timeouts.ReadTotalTimeoutMultiplier = value; }
        /// <summary>
        /// Write timeout per byte. Used to calculate total timeout multiplying it by the number of bytes to write and 'WriteTotalTimeoutMultiplier'. Use 0 in this and 'WriteTotalTimeoutMultiplier' for an infinite timeout.
        /// </summary>
        public uint WriteTotalTimeoutConstant { get => timeouts.WriteTotalTimeoutConstant; set => timeouts.WriteTotalTimeoutConstant = value; }
        /// <summary>
        /// Write timeout multiplier. Used to calculate total timeout multiplying it by the number of bytes to write and 'WriteTotalTimeoutConstant'. Use 0 in this and 'WriteTotalTimeoutConstant' for an infinite timeout.
        /// </summary>
        public uint WriteTotalTimeoutMultiplier { get => timeouts.WriteTotalTimeoutMultiplier; set => timeouts.WriteTotalTimeoutMultiplier = value; }
        /// <summary>
        /// Indicates that the timeout will not be used and the waiting time will be infinite.
        /// </summary>
        public const uint InfiniteTimeout = 0;
        /// <summary>
        /// Max value for a timeout.
        /// </summary>
        public const uint MaxTimeout = uint.MaxValue;

        /// <summary>
        /// SerialPort class constructor.
        /// </summary>
        /// <param name="portName">The COM portname or COM device path</param>
        /// <param name="baudRate">The baud rate used for the communication</param>
        /// <param name="deviceName">The friendly name of the device connected to the port (Optional)</param>
        public SerialPort(string portName, BaudRates baudRate, string deviceName = "Unknown")
        {
            Name = deviceName;
            PortName = portName;
            BaudRate = baudRate;

            timeouts.ReadIntervalTimeout = 0;
            timeouts.ReadTotalTimeoutConstant = 10;
            timeouts.ReadTotalTimeoutMultiplier = 1;
            timeouts.WriteTotalTimeoutConstant = 10;
            timeouts.WriteTotalTimeoutMultiplier = 1;
        }
        /// <summary>
        /// SerialPort class constructor.
        /// </summary>
        /// <param name="info">The COM device info struct provided for 'GetSerialDevices' function</param>
        /// <param name="baudRate">The baud rate used for the communication</param>
        public SerialPort(SerialDeviceInfo info, BaudRates baudRate)
            : this(info.PortName,baudRate,info.DeviceName) { }

        private static SafeFileHandle OpenHandle(string devicePathName)
        {
            SafeFileHandle hidHandle;
            try
            {
                hidHandle = CreateFile(devicePathName, GENERIC_READ | GENERIC_WRITE, 0, IntPtr.Zero, OPEN_EXISTING, 0, 0);
            }
            catch (Exception)
            {
                throw;
            }
            return hidHandle;
        }
        /// <summary>
        /// Connects the port and set it up to use.
        /// </summary>
        /// <returns>The value of 'IsConnected' property. If is true the device has been successfully connected and setup.</returns>
        public bool Connect()
        {
            if (IsConnected)
                return true;

            port = OpenHandle(PortName);

            IsConnected = !port.IsInvalid;
            if(IsConnected)
            {
                GetCommState(port.DangerousGetHandle(),ref dcb);
                UpdateConfig();
            }
            return IsConnected;
        }
        /// <summary>
        /// Disconnects the port and dispose the port handler. Use 'Connect' function to reconnect the port.
        /// </summary>
        /// <returns>The negated value of 'IsConnected' property. If is true the device has been successfully disconnected.</returns>
        public bool Disconnect()
        {
            if (!IsConnected)
                return false;

            if (port != null && !port.IsInvalid)
            {
                port.Close();
            }
            port = null;

            IsConnected = false;
            return !IsConnected;
        }
        /// <summary>
        /// Updates the port configs and timeouts setted from object properties. The ports needs to be connected to correctly update config.
        /// </summary>
        public void UpdateConfig()
        {
            if (IsConnected && port != null)
            {
                dcb.Parity = Parity;
                dcb.StopBits = StopBits;
                dcb.BaudRate = BaudRate;
                dcb.ByteSize = ByteSize;
                dcb.RtsControl = RtsControl;
                dcb.DtrControl = DtrControl;
                SetCommState(port.DangerousGetHandle(), ref dcb);
                SetCommTimeouts(port.DangerousGetHandle(), ref timeouts);
            }
        }
        /// <summary>
        /// Clear the receive buffer of the port
        /// </summary>
        /// <returns>True if the operation was successful</returns>
        public bool FlushRXBuffer() => PurgeCom(true, false, false, false);
        /// <summary>
        /// Clear the transmit buffer of the port
        /// </summary>
        /// <returns>True if the operation was successful</returns>
        public bool FlushTXBuffer() => PurgeCom(false, true, false,false);
        /// <summary>
        /// Cancel transmit and receive current operations of the port
        /// </summary>
        /// <param name="rx">If is true cancel revice operations</param>
        /// <param name="tx">If is true cancel transmit operations</param>
        /// <returns>True if the operation was successful</returns>
        public bool CancelCurrentIO(bool rx = true, bool tx = true) => PurgeCom(false, false,rx, tx);
        /// <summary>
        /// Clears/Cancel Buffers/Operations of the port
        /// </summary>
        /// <param name="rxBuf">If is true clears receive buffer of the port</param>
        /// <param name="txBuf">If is true clears transmit buffer of the port</param>
        /// <param name="rxOps">If is true abort current receive operations of the port</param>
        /// <param name="txOps">If is true abort current transmit operations of the port</param>
        /// <returns></returns>
        public bool PurgeCom(bool rxBuf = true, bool txBuf = true, bool rxOps = true, bool txOps = true)
        {
            if (IsConnected && port != null)
                return PurgeComm(port.DangerousGetHandle(), (txBuf ? PurgeEnum.PURGE_TXCLEAR : 0) | (rxBuf ? PurgeEnum.PURGE_RXCLEAR : 0)
                    | (txOps ? PurgeEnum.PURGE_TXABORT : 0) | (rxOps ? PurgeEnum.PURGE_RXABORT : 0));

            return false;
        }
        /// <summary>
        /// Write a byte buffer to the port.
        /// </summary>
        /// <param name="buffer">Byte array to be written into the port</param>
        /// <returns>The number of bytes written</returns>
        public uint Write(byte[] buffer)
        {
            uint bytesWritten = 0;
            if (IsConnected && port != null)
            {
                WriteFile(port.DangerousGetHandle(), buffer, (uint)buffer.Length, out bytesWritten, IntPtr.Zero);
            }
            return bytesWritten;
        }
        /// <summary>
        /// Reads a byte buffer from the port.
        /// </summary>
        /// <param name="buffer">The output byte array</param>
        /// <param name="count">The number of bytes to read from the port</param>
        /// <returns>The number of bytes readed</returns>
        public uint Read(out byte[] buffer, int count)
        {
            uint bytesReaded = 0;
            if (IsConnected && port != null)
            {
                buffer = new byte[count];
                ReadFile(port.DangerousGetHandle(), buffer, (uint)buffer.Length, out bytesReaded, IntPtr.Zero);
            }
            else
                buffer= Array.Empty<byte>();
            return bytesReaded;
        }
        /// <summary>
        /// Writes a structure to the port.
        /// </summary>
        /// <typeparam name="T">Type of the structure to be written</typeparam>
        /// <param name="buffer">The structure to be written</param>
        /// <returns>The number of bytes written</returns>
        public uint Write<T>(T buffer) where T : struct
        {
            uint bytesWritten = 0;
            IntPtr ptr = Marshal.AllocHGlobal(Marshal.SizeOf<T>());
            Marshal.StructureToPtr(buffer, ptr, true);
            if (IsConnected && port != null)
            {
                WriteFile(port.DangerousGetHandle(), ptr, (uint)Marshal.SizeOf<T>() , out bytesWritten, IntPtr.Zero);
            }
            Marshal.FreeHGlobal(ptr);
            return bytesWritten;
        }
        /// <summary>
        /// Reads a structure from the port.
        /// </summary>
        /// <typeparam name="T">Type of the structure to be read</typeparam>
        /// <param name="buffer">The readed structure</param>
        /// <returns>The number of bytes readed</returns>
        public uint Read<T>(out T buffer) where T : struct
        {
            uint bytesReaded = 0;
            IntPtr ptr = Marshal.AllocHGlobal(Marshal.SizeOf<T>());
            if (IsConnected && port != null)
            {
                ReadFile(port.DangerousGetHandle(), ptr, (uint)Marshal.SizeOf<T>(), out bytesReaded, IntPtr.Zero);
            }
            buffer = Marshal.PtrToStructure<T>(ptr);
            Marshal.FreeHGlobal(ptr);
            return bytesReaded;
        }
        private COMMPROP commprop;
        private COMMPROP? COMMProperties
        {
            get
            {
                if (IsConnected && port != null)
                {
                    GetCommProperties(port.DangerousGetHandle(), ref commprop);
                    return commprop;
                }
                else
                    return null;
            }
        }
        private COMSTAT? COMStats
        {
            get
            {
                if (IsConnected && port != null)
                {
                    ClearCommError(port.DangerousGetHandle(), out uint errors, out COMSTAT comStats);
                    return comStats;
                }
                else
                    return null;
            }
        }
        /// <summary>
        /// Gets the bytes stored on the recieve buffer of the port. Returns 0 if there are no bytes in the buffer or the port is disconnected.
        /// </summary>
        public uint BytesToRead => (COMStats?.cbInQue) ?? 0;
        /// <summary>
        /// Gets the bytes stored on the transmit buffer of the port. Returns 0 if there are no bytes in the buffer or the port is disconnected.
        /// </summary>
        public uint BytesToWrite => (COMStats?.cbOutQue) ?? 0;

        /// <summary>
        /// Query the connected COM ports using WMI queries
        /// </summary>
        /// <returns>The available COM ports as SerialDeviceInfo structs</returns>
        public static SerialDeviceInfo[] GetSerialDevices() => COMDeviceScanner.GetSerialDevices();
    }
    /// <summary>
    /// Parity protocols for Native Windows Serialport
    /// </summary>
    public enum Parity : byte
    {
#pragma warning disable CS1591 // Falta el comentario XML para el tipo o miembro visible públicamente
        None = 0,
        Odd = 1,
        Even = 2,
        Mark = 3,
        Space = 4,
#pragma warning restore CS1591 // Falta el comentario XML para el tipo o miembro visible públicamente
    }
    /// <summary>
    /// Stop bits count for Native Windows Serialport
    /// </summary>
    public enum StopBits : byte
    {
#pragma warning disable CS1591 // Falta el comentario XML para el tipo o miembro visible públicamente
        One = 0,
        OnePointFive = 1,
        Two = 2
#pragma warning restore CS1591 // Falta el comentario XML para el tipo o miembro visible públicamente
    }
    /// <summary>
    /// Request To Send enum
    /// </summary>
    public enum RtsControl : int
    {
        /// <summary>
        /// Disables the RTS line when the device is opened and leaves it disabled.
        /// </summary>
        Disable = 0,

        /// <summary>
        /// Enables the RTS line when the device is opened and leaves it on.
        /// </summary>
        Enable = 1,

        /// <summary>
        /// Enables RTS handshaking. The driver raises the RTS line when the "type-ahead" (input) buffer
        /// is less than one-half full and lowers the RTS line when the buffer is more than
        /// three-quarters full. If handshaking is enabled, it is an error for the application to
        /// adjust the line by using the EscapeCommFunction function.
        /// </summary>
        Handshake = 2,

        /// <summary>
        /// Specifies that the RTS line will be high if bytes are available for transmission. After
        /// all buffered bytes have been sent, the RTS line will be low.
        /// </summary>
        Toggle = 3
    }
    /// <summary>
    /// Data Terminal Ready enum
    /// </summary>
    public enum DtrControl : int
    {
        /// <summary>
        /// Disables the DTR line when the device is opened and leaves it disabled.
        /// </summary>
        Disable = 0,

        /// <summary>
        /// Enables the DTR line when the device is opened and leaves it on.
        /// </summary>
        Enable = 1,

        /// <summary>
        /// Enables DTR handshaking. If handshaking is enabled, it is an error for the application to adjust the line by
        /// using the EscapeCommFunction function.
        /// </summary>
        Handshake = 2
    }
    /// <summary>
    /// Enumeration of standard baud rate values. Use a cast to this enum with the desired number if it isn't on the enumeration.
    /// </summary>
    public enum BaudRates : uint
    {
#pragma warning disable CS1591 // Falta el comentario XML para el tipo o miembro visible públicamente
        BR110 = 110,
        BR300 = 300,
        BR600 = 600,
        BR1200 = 1200,
        BR2400 = 2400,
        BR4800 = 4800,
        BR9600 = 9600,
        BR14400 = 14400,
        BR19200 = 19200,
        BR38400 = 38400,
        BR56000 = 56000,
        BR57600 = 57600,
        BR115200 = 115200,
        BR128000 = 128000,
        BR256000 = 256000
#pragma warning restore CS1591 // Falta el comentario XML para el tipo o miembro visible públicamente
    }
}
