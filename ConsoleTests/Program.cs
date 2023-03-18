
using FireLibs.IO.HID;
using FireLibs.IO.COMPorts;
using System.Management;
using System.Xml.Linq;
using System.Security.Cryptography;
string SearchStr(string vid, string pid) => $"HID%VID_%{vid}_PID_{pid}%";

//string wmiQuery = @"SELECT * FROM Win32_PnPEntity WHERE ClassGuid=""{4d36e978-e325-11ce-bfc1-08002be10318}""";
//string wmiQuery = @"SELECT DeviceID, Name FROM Win32_SerialPort";
//string wmiQuery = @"SELECT * FROM Win32_USBControllerdevice";//
string wmiQuery = $@"SELECT * FROM Win32_PnPEntity WHERE DeviceID LIKE '{SearchStr("", "09cc")}'";//'%PID_B031%' OR DeviceID LIKE '%PID_09cc%'
//Name, DeviceID, PNPClass
//HID\VID_0583&PID_B031\7&C50BC48&4&0000
//HID\{00001124-0000-1000-8000-00805F9B34FB}_VID&0002054C_PID&09CC\9&2005301&25&0000
//\\?\HID\{00001124-0000-1000-8000-00805F9B34FB}_VID&0002054C_PID&09CC/9&2005301&25&0000

//USB
//\\?\hid#vid_054c&pid_09cc&mi_03#8&56c1193&0&0000#{4d1e55b2-f16f-11cf-88cb-001111000030}
//BLUETOOTH
//\\?\hid#{00001124-0000-1000-8000-00805f9b34fb}_vid&0002054c_pid&09cc#9&2005301&25&0000#{4d1e55b2-f16f-11cf-88cb-001111000030}

//Console.WriteLine(HidDevice.HidClassGuid);

ManagementObjectSearcher searcher = new ManagementObjectSearcher(wmiQuery);
ManagementObjectCollection objCollection = searcher.Get();
foreach (var obj in objCollection)
{
    //Console.WriteLine($"{obj["DeviceID"]}: {obj["Name"]}");
    foreach (var prop in obj.Properties ){
        if (prop.Value is Array)
        {
            foreach(object s in prop.Value as Array)
            {
                Console.WriteLine($"{prop.Name}: {s}");
            }
        }
        else
        {
            Console.WriteLine($"{prop.Name}: {prop.Value}");
        }

    }
    Console.WriteLine();
}
