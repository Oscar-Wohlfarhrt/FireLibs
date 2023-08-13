using FireLibs.IO.COMPorts.Win;
using System.Runtime.InteropServices;

/* Program to test library functions and check it's functionalities */

SerialPort port = new("COM5", BaudRates.BR57600);

port.Connect();
while ( true && port.IsConnected)
{
    port.CancelCurrentIO(tx: false);
    port.FlushRXBuffer();

    port.Write(new byte[] { 0x01 });

    Thread.Sleep(100);
    var btr = port.BytesToRead;

    port.Read(out COMInputReport report);
    Console.Clear();
    Console.WriteLine(btr);
    Console.WriteLine(port.BytesToRead);
    Console.WriteLine(report);
}

port.Disconnect();


[StructLayout(LayoutKind.Sequential, Pack = 1)]
internal struct COMGetInput
{
    byte request = 0x01;
    public COMGetInput(bool init=true) { }
}

[StructLayout(LayoutKind.Sequential, Pack = 1, Size = 64)]
internal struct COMInputReport
{
    public byte id = 0;
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 12)]
    public short[] Axis = new short[12];
    public uint Buttons = 0;
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
    public ushort[] Pov = new ushort[2];
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
    public short[] Accel = new short[3];
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
    public short[] Gyro = new short[3];

    public COMInputReport() { }
    public override string ToString()
    {
        string outStr="";
        for(int i =0;i<Axis.Length;i++)
        {
            outStr+=$"Axis[{i}]: {Axis[i]}\n";
        }
        return outStr;
    }
}