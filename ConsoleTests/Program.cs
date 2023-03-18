
using FireLibs.IO.HID;
using FireLibs.IO.COMPorts;
using System.Management;
using System.Xml.Linq;
using System.Security.Cryptography;
using System;


void GetHidDevsWMI(int vid=0, int pid=0)
{
    string wmiQuery = $@"SELECT DeviceID,Description FROM Win32_PnPEntity WHERE DeviceID LIKE '{SearchStr(vid>0?$"{vid:X4}":"", pid > 0 ? $"{pid:X4}" : "")}'";
    ManagementObjectSearcher searcher = new ManagementObjectSearcher(wmiQuery);
    ManagementObjectCollection objCollection = searcher.Get();
    foreach (var obj in objCollection)
    {
        //Console.WriteLine($"{obj["DeviceID"]}: {obj["Name"]}");
        foreach (var prop in obj.Properties)
        {
            if (prop.Value is Array)
            {
                foreach (object s in prop.Value as Array)
                {
                    Console.WriteLine($"{prop.Name}: {s}");
                }
            }
            else
            {
                if (prop.Name == "DeviceID")
                    Console.WriteLine($"\\\\?\\{prop.Value.ToString().ToLower().Replace("\\", "#")}#{{4d1e55b2-f16f-11cf-88cb-001111000030}}");
                else
                    Console.WriteLine($"{prop.Name}: {prop.Value}");
            }

        }
        Console.WriteLine();
    }
}

//string wmiQuery = @"SELECT * FROM Win32_PnPEntity WHERE ClassGuid=""{4d36e978-e325-11ce-bfc1-08002be10318}""";
//string wmiQuery = @"SELECT DeviceID, Name FROM Win32_SerialPort";
//string wmiQuery = @"SELECT * FROM Win32_USBControllerdevice";//
//'%PID_B031%' OR DeviceID LIKE '%PID_09cc%'

//Name, DeviceID, PNPClass
//HID\VID_0583&PID_B031\7&C50BC48&4&0000
//HID\{00001124-0000-1000-8000-00805F9B34FB}_VID&0002054C_PID&09CC\9&2005301&25&0000
//\\?\HID\{00001124-0000-1000-8000-00805F9B34FB}_VID&0002054C_PID&09CC/9&2005301&25&0000

//USB
//\\?\hid#vid_054c&pid_09cc&mi_03#8&56c1193&0&0000#{4d1e55b2-f16f-11cf-88cb-001111000030}
//BLUETOOTH
//\\?\hid#{00001124-0000-1000-8000-00805f9b34fb}_vid&0002054c_pid&09cc#9&2005301&25&0000#{4d1e55b2-f16f-11cf-88cb-001111000030}

//Console.WriteLine(HidDevice.HidClassGuid);

//GetHidDevsWMI(0x054c);

/*foreach (var dev in HidDevice.Enumerate(0x1234))
{
    Console.WriteLine(dev.Information.Path + "\n" + dev.Information.Description + "\n");
}*/

string SearchStr(string vid, string pid) => $"HID%VID_%{vid}_PID_{pid}%";
string BuildQuery(int? vid,int[]? pid)
{
    string query = "SELECT DeviceID,Description FROM Win32_PnPEntity WHERE DeviceID LIKE 'HID%'";
    //string? vidStr = vid==null?null:$"{vid:X4}";
    IEnumerable<string>? pidStr = pid?.Select(i=>$"{i:X4}");

    if (vid != null)
        query += $" AND DeviceID LIKE 'HID%VID_{vid:X4}%'";

    if (pidStr != null)
        query += $" AND (DeviceID LIKE 'HID%PID_{string.Join("%' OR DeviceID LIKE 'HID%PID_", pidStr)}%')";

    return query;
}
//DeviceID,Description,HardwareID
string wmiQuery = "SELECT DeviceID,Description,HardwareID FROM Win32_PnPEntity WHERE DeviceID LIKE 'HID%'";
/*BuildQuery(null, new[]{
0x09cc,
0xBEAD,
});*/
ManagementObjectSearcher searcher = new ManagementObjectSearcher(wmiQuery);
ManagementObjectCollection objCollection = searcher.Get();
foreach (var obj in objCollection)
{
    //Console.WriteLine($"{obj["DeviceID"]}: {obj["Name"]}");
    foreach (var prop in obj.Properties)
    {
        if (prop.Value is Array)
        {
            foreach (object s in prop.Value as Array)
            {
                Console.WriteLine($"{prop.Name}: {s}");
            }
        }
        else
        {
            if (prop.Name == "DeviceID")
                Console.WriteLine($"\\\\?\\{prop.Value.ToString().ToLower().Replace("\\", "#")}#{{4d1e55b2-f16f-11cf-88cb-001111000030}}");
            else
                Console.WriteLine($"{prop.Name}: {prop.Value}");
        }

    }
    Console.WriteLine();
}