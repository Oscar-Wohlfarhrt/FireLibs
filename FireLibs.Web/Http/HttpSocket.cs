using System;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace FireLibs.Web.Http
{
    internal static class HttpSocketExtensions
    {
        public static byte[] ReadBytes(this Socket soc, int count)
        {
            byte[] buffer = new byte[count];
            soc.Receive(buffer);
            return buffer;
        }
        public static byte[] ReadAllBytes(this Socket soc)
        {
            List<byte> bytes = new();
            while (soc.Available > 0)
                bytes.AddRange(soc.ReadBytes(soc.Available));

            return bytes.ToArray();
        }
        public static string ReadString(this Socket soc) => Encoding.UTF8.GetString(soc.ReadAllBytes());
        /*{
            string request = "";
            while (soc.Available > 0)
                request += Encoding.UTF8.GetString(soc.ReadBytes(soc.Available));

            return request;
        }*/
        public static bool ReceiveHttpRequest(this Socket soc, int requestTimeout, out HttpRequest request)
        {
            DateTime now = DateTime.Now.AddMilliseconds(requestTimeout);

            string req = "";
            while (now >= DateTime.Now)
            {
                req += soc.ReadString();
                if(HttpRequest.TryParseHttpRequest(req, out request, (IPEndPoint?)soc.RemoteEndPoint))
                    return true;
            }
            request = HttpRequest.EmptyRequest;
            return false;
        }

        public static void SendHttpHeaders(this Socket soc, HttpStatus status, Dictionary<string, List<string>> headers, string version = "HTTP/1.1") =>
            soc.SendContent(status.GetFullHeader(headers, version));
        public static void SendContent(this Socket soc, string content) =>
            soc.Send(content.GetBytes());
        public static void SendServerEvent(this Socket soc, string data, string eventType = "") =>
            soc.SendContent($"{(eventType!=""?$"event: {eventType}\n":"")}data: {data.Replace("\n","\ndata:")}\n\n");
        public static void SendHttpResponse(this Socket soc, HttpResponse response)
        {
            soc.SendHttpHeaders(response.Status,response.Headers,response.Version);
            soc.SendContent(response.Content);
        }
        public static void SendResponse(this HttpResponse response, Socket socket) => socket.SendHttpResponse(response);

        public static byte[] GetBytes(this string str) =>
            Encoding.UTF8.GetBytes(str);
    }
}
