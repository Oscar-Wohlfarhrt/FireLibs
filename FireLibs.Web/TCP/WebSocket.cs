using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using FireLibs.Web.Http;
using System.ComponentModel.Design;
using System.Diagnostics;

namespace FireLibs.Web.Http
{
    public class WebSocket
    {
        Socket? client;

        internal Socket? Client { set=>client=value; }
        public int Available => client?.Available ?? 0;

        public bool IsConnected { 
            get {
                if (client?.Connected ?? false)
                    return true;
                else if (client != null)
                    client = null;
                return false;
            }
        }

        public void SendText(string message)
        {
            if (IsConnected)
            {
                Random rnd = new Random();
                byte[] mask = new byte[4];
                rnd.NextBytes(mask);

                List<byte> data = new();
                data.Add(129);

                //int length = 0;
                switch (message.Length)
                {
                    case int length when length < 126:
                        data.Add((byte)(0x80 | length));
                        break;
                    case int length when length < ushort.MaxValue:
                        data.Add(126);
                        data.AddRange(BitConverter.GetBytes(message.Length)[..2].Reverse().ToArray());
                        break;
                    default:
                        data.Add(127);
                        data.AddRange(new byte[] { 0, 0, 0, 0 });
                        data.AddRange(BitConverter.GetBytes(message.Length).Reverse().ToArray());
                        break;
                }
                //data.AddRange(mask);
                data.AddRange(message.GetBytes());

                client?.Send(data.ToArray());
            }
        }
        public string ReadText()
        {
            if (IsConnected)
            {
                //mainWebsocket.send("Hello world")
                Debug.WriteLine($"Reading Frames");
                byte? fOpCode = null;
                List<byte> data = new();
                bool cont = false;
                do
                {
                    cont = !ReadFrame(out byte[] frame, out byte opCode);
                    Debug.WriteLine($"Frame Readed: {frame.Length} : {(int)opCode}");
                    fOpCode ??= opCode;
                    data.AddRange(frame);
                } while (cont);

                Debug.WriteLine($"OpCode: {fOpCode}");

                if (fOpCode != null && fOpCode == 1)
                {
                    string output = Encoding.UTF8.GetString(data.ToArray());
                    Debug.WriteLine(output);
                    return output;
                }

                return "";
            }

            return "";
        }
        private bool ReadFrame(out byte[] frame, out byte opCode)
        {
            Debug.WriteLine($"{client?.Available}");
            byte[] buffer = new byte[2];
            if(client?.Receive(buffer) == 2)
            {
                Debug.WriteLine($"Readed Header");
                opCode = (byte)(buffer[0] & 0x0F);
                long length = 0;
                byte[] bLength;

                switch (buffer[1] & 0x7F)
                {
                    case 126:
                        bLength = new byte[2];
                        if(client.Receive(bLength) == 2)
                            length = BitConverter.ToInt16(bLength.Reverse().ToArray());
                        break;
                    case 127:
                        bLength = new byte[4];
                        if (client.Receive(bLength) == 4)
                            length = BitConverter.ToInt64(bLength.Reverse().ToArray());
                        break;
                    default:
                        length = buffer[1] & 0x7F;
                        break;
                }

                Debug.WriteLine($"Length: {length}");
                bLength = new byte[4];
                client?.Receive(bLength);
                frame = new byte[length];
                Debug.WriteLine($"Decoding");
                client?.Receive(frame);
                frame = MessageCoder(frame,bLength);

                return (buffer[0] | 0x80) != 0;
            }
            frame = Array.Empty<byte>();
            opCode = 0;
            return true;
        }
        private byte[] MessageCoder(byte[] content, byte[] mask)
        {
            for (int i = 0; i < content.Length; i++)
                content[i] = (byte)(content[i] ^ mask[i % 4]);

            return content;
        }
    }
}
