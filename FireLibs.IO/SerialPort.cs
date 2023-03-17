using System.Diagnostics;
using System.Management;

namespace FireLibs.IO.COMPorts
{
    public struct SerialDeviceInfo
    {
        public string PortName { get; private set; }
        public string DeviceName { get; private set; }

        public SerialDeviceInfo(string portName) : this(portName, portName) { }
        public SerialDeviceInfo(string portName, string devName)
        {
            PortName = portName;
            DeviceName = devName;
        }
        public override string ToString()
        {
            return $"Name: {DeviceName} / Port: {PortName}";
        }
    }

    public class SerialPort
    {
        private System.IO.Ports.SerialPort port = new();

        public string PortName { get => port.PortName; }
        public int BaudRate { get => port.BaudRate; set => port.BaudRate = value; }
        public string DeviceName { get; private set; }
        public bool IsConnected { get; private set; }

        public int ReadTimeout { get => port.ReadTimeout; set => port.ReadTimeout = value; }
        public int WriteTimeout { get => port.WriteTimeout; set => port.WriteTimeout = value; }

        public int BytesToRead => port.BytesToRead;
        public int BytesToWrite => port.BytesToWrite;

        public System.IO.Ports.SerialPort SystemPort => port;
        public Stream BaseStream => port.BaseStream;

        public SerialPort(SerialDeviceInfo devInfo, int baudRate = 9600) : this(devInfo.PortName, baudRate, devInfo.DeviceName) { }
        public SerialPort(string portName, int baudRate = 9600, string deviceName = "Unknown")
        {
            port = new(portName, baudRate);
            DeviceName = deviceName;
        }
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
        public int ReadByte() => port.ReadByte();
        public string ReadExisting() => port.ReadExisting();
        public int Read(char[] buffer, int offset, int count) => port.Read(buffer,offset,count);
        public int Read(byte[] buffer, int offset, int count) => port.Read(buffer, offset, count);
        public bool ReadCount(byte[] buffer,int offset,int count)
        {
            Stopwatch watch = new Stopwatch();
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
        public void Write(string text) => port.Write(text);
        public void WriteLine(string text) => port.WriteLine(text);
        public void Write(char[] buffer, int offset, int count)=>port.Write(buffer,offset,count);
        public void Write(byte[] buffer, int offset, int count) => port.Write(buffer, offset, count);

        public const int InfinityTimeout = System.IO.Ports.SerialPort.InfiniteTimeout;
        public static string[] GetPortNames() => System.IO.Ports.SerialPort.GetPortNames();
#if _WINDOWS
        public static SerialDeviceInfo[] GetSerialDevices()
        {
            List<SerialDeviceInfo> infos = new();

            string wmiQuery = @"SELECT DeviceID, Name FROM Win32_SerialPort";

            ManagementObjectSearcher searcher = new (wmiQuery);
            ManagementObjectCollection objCollection = searcher.Get();


            foreach (ManagementObject obj in objCollection)
            {
                string? port = obj["DeviceID"].ToString();
                string? name = obj["Name"].ToString();

                if (port != null)
                    infos.Add(new(port, (name!=null?name:port)));
            }

            return infos.ToArray();
        }
#else
        public static SerialDeviceInfo[] GetSerialDevices() => GetPortNames()
                .Select((p) => new SerialDeviceInfo(p, "Unknown")).ToArray();
#endif
    }
}