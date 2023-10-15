using FireLibs.Web.TCP;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace FireLibs.Web.Http
{
    public class HttpRequestEventArgs : EventArgs
    {
        public string IpUrl { get; private set; }
        public string Url { get => IpUrl + RelativeUrl; }
        public string RelativeUrl { get; private set; }
        public HttpRequestEventArgs(string ipUrl, string relativeUrl)
        {
            IpUrl = ipUrl;
            RelativeUrl = relativeUrl;
        }
    }

    public enum HttpStatus
    {
        OK,
        MovedPermanently,
        BadRequest,
        Unauthorized,
        Forbidden,
        NotFound,
        InternalServerError,
        ServiceUnavailable
    }

    public static class HttpHeaders
    {
        private const string eol = "\r\n";
        private static readonly string[] Headers = {
        "HTTP/1.1 200 OK",
        "HTTP/1.1 301 Moved Permanently",
        "HTTP/1.1 400 Bad Request",
        "HTTP/1.1 401 Unauthorized",
        "HTTP/1.1 403 Forbidden",
        "HTTP/1.1 404 Not Found",
        "HTTP/1.1 500 Internal Server Error",
        "HTTP/1.1 503 Service Unavailable",
    };

    public static string GetFullHeader(this HttpStatus status, string[]? extraHeaders = null) =>
            Headers[(int)status] + eol + (extraHeaders != null ? string.Join(eol, extraHeaders) + eol : "") + eol;
    }

    public class DSHttpServer
    {
        TcpServer server;
        public int RequestTimeout { get; set; }

        public delegate HttpStatus GetAction(string op, IPAddress? ip, string path, out byte[] body, out string[]? extraHeaders);
        public GetAction? GetResponse { get; set; }

        public DSHttpServer(int port, int timeout = 1000) : this(IPAddress.Any, port, timeout) { }
        public DSHttpServer(IPAddress ip, int port, int timeout = 1000)
        {
            server = new TcpServer(ip, port);
            server.OnClientConnected += ClientConnected;
            RequestTimeout = timeout;
        }
        public void Start() => server.Start();
        public void Stop() => server.Stop();
        private static HttpStatus DefaultResponse(string op, IPAddress? ip, string path, out byte[] body, out string[]? extraHeaders)
        {
            body = Array.Empty<byte>();
            extraHeaders = null;
            return HttpStatus.NotFound;
        }
        
        private void ClientConnected(object? sender, ClientConnectedEventArgs e)
        {
            TcpClient cli = e.Client;
            NetworkStream stream = cli.GetStream();
            DateTime now = DateTime.Now.AddMilliseconds(RequestTimeout);

            while (cli.Available < 3 && now >= DateTime.Now) ;

            if (cli.Available > 0)
            {
                byte[] buffer = new byte[cli.Available];

                stream.Read(buffer, 0, buffer.Length);

                string request = Encoding.UTF8.GetString(buffer);
                Console.WriteLine(request.Replace("\r\n", " >>\r\n"));

                Match match = Regex.Match(request, @"^(.+) (.+) HTTP\/...");

                string operation = "", relUrl = "";
                if (match.Groups.Count >= 2)
                {
                    operation = match.Groups[1].Value;
                    relUrl = match.Groups[2].Value;
                }

                HttpStatus status = (GetResponse ?? DefaultResponse)
                    (operation, e.LocalIp, relUrl, out byte[] body, out string[]? extraHeaders);

                bool writeBody = false;
                string response = "";
                switch (operation)
                {
                    case "GET":
                        response = status.GetFullHeader(extraHeaders);
                        writeBody = true;
                        break;
                    case "HEAD":
                        response = status.GetFullHeader(extraHeaders);
                        break;
                    default:
                        response = HttpStatus.BadRequest.GetFullHeader(extraHeaders);
                        break;
                }
                Console.WriteLine(response.Replace("\r\n", " >>\r\n"));

                byte[] outBuffer = Encoding.UTF8.GetBytes(response);
                stream.Write(outBuffer, 0, outBuffer.Length);

                if (writeBody)
                    stream.Write(body, 0, body.Length);
            }

            cli.Close();
        }
    }
}
