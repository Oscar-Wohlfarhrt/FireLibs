using System;
using System.IO.Ports;
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
        public int Timeout { get; set; } = 0;
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
        public bool Read(byte[] buffer,int offset,int count)
        {
            DateTime timeout = DateTime.Now.AddMilliseconds(Timeout);
            while (port.BytesToRead < count && DateTime.Now < timeout) ;
            if (DateTime.Now < timeout)
            {
                port.BaseStream.Read(buffer, offset, count);
                return true;
            }
            return false;
        }
#if _WINDOWS
        public static SerialDeviceInfo[] GetSerialDevices()
        {
            string wmiQuery = @"SELECT DeviceID, Name FROM Win32_SerialPort";

            ManagementObjectSearcher searcher = new (wmiQuery);
            ManagementObjectCollection objCollection = searcher.Get();

            List<SerialDeviceInfo> infos = new();

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
        public static SerialDeviceInfo[] GetSerialDevices() => System.IO.Ports.SerialPort.GetPortNames()
                .Select((p) => new SerialDeviceInfo(p, "Unknown")).ToArray();
#endif
    }
}