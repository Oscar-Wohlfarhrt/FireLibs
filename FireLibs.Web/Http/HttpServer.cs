using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using FireLibs.Web.TCP;
using FireLibs.Logging;

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
        Continue = 100,
        SwitchingProtocols = 101,
        OK = 200,
        Created = 201,
        Accepted = 202,
        NonAuthoritativeInformation = 203,
        NoContent = 204,
        ResetContent = 205,
        PartialContent = 206,
        MultipleChoices = 300,
        MovedPermanently = 301,
        MovedTemporarily = 302,
        SeeOther = 303,
        NotModified = 304,
        UseProxy = 305,
        BadRequest = 400,
        Unauthorized = 401,
        PaymentRequired = 402,
        Forbidden = 403,
        NotFound = 404,
        MethodNotAllowed = 405,
        NoneAcceptable = 406,
        ProxyAuthenticationRequired = 407,
        RequestTimeout = 408,
        Conflict = 409,
        Gone = 410,
        LengthRequired = 411,
        UnlessTrue = 412,
        InternalServerError = 500,
        NotImplemented = 501,
        BadGateway = 502,
        ServiceUnavailable = 503,
        GatewayTimeout = 504,
    }

    public static class HttpHeaders
    {
        private const string eol = "\r\n";
        public static string GetFullHeader(this HttpStatus status, Dictionary<string, string[]> extraHeaders, string httpVersion = "HTTP/1.1")
        {
            string[] headers = extraHeaders.SelectMany(kv => kv.Value.Select(v => $"{kv.Key}: {v}{eol}")).ToArray();
            return $"{httpVersion} {(int)status} {status.GetStatusPhrase()}{eol}{string.Join("",headers)}{eol}";
        }
        public static string GetStatusPhrase(this HttpStatus status)=>
            status.ToString().ToSentenceCase();
        private static string ToSentenceCase(this string str) =>
            Regex.Replace(str, "[a-z][A-Z]", m => $"{m.Value[0]} {m.Value[1]}");
    }

    public class HttpServer
    {
        SocketServer server;
        public int RequestTimeout { get; set; }

        public delegate HttpStatus GetAction(string op, IPAddress? ip, string path, Dictionary<string, string[]> inHeaders, out byte[] body, out Dictionary<string, string[]> outHeaders);
        public GetAction? GetResponse { get; set; }
        private Logger? _logger;

        public HttpServer(int port, int timeout = 1000, Logger? logger = null) : this(IPAddress.Any, port, timeout, logger) { }
        public HttpServer(IPAddress ip, int port, int timeout = 1000, Logger? logger = null)
        {
            server = new(SocketType.Stream, ProtocolType.Tcp, new IPEndPoint(ip, port));
            server.OnSocketConnected += ClientConnected;
            RequestTimeout = timeout;
            _logger = logger;
        }
        public void Start() => server.Start();
        public void Stop() => server.Stop();
        private static HttpStatus DefaultResponse(string op, IPAddress? ip, string path, Dictionary<string, string[]> inHeaders, out byte[] body, out Dictionary<string, string[]> outHeaders)
        {
            body = Array.Empty<byte>();
            outHeaders = new();
            return HttpStatus.NotImplemented;
        }
        
        private void ClientConnected(object? sender, SocketConnectedEventArgs e)
        {
            Socket cli = e.Client;
            
            DateTime now = DateTime.Now.AddMilliseconds(RequestTimeout);

            while (cli.Available < 3 && now >= DateTime.Now) ;

            if (cli.Available > 0)
            {
                byte[] buffer = new byte[cli.Available];

                cli.Receive(buffer);

                string request = Encoding.UTF8.GetString(buffer);
                _logger?.LogDebug("WebServer Log:\n" + request.Replace("\r\n", " >>\n"));

                Match match = Regex.Match(request, @"^(.+?) (.+?) (HTTP\/.+?)\r\n((?:.*?\r\n)*?)?\r\n");

                string operation = "", relUrl = "", httpVersion = "";
                Dictionary<string, string[]> headers = new();
                if (match.Groups.Count >= 3)
                {
                    operation = match.Groups[1].Value;
                    relUrl = match.Groups[2].Value;
                    httpVersion = match.Groups[3].Value;
                }
                if (match.Groups.Count >= 4)
                {
                    string[] strHeaders = match.Groups[4].Value.Split("\r\n",StringSplitOptions.RemoveEmptyEntries);
                    foreach (string strHeader in strHeaders)
                    {
                        string[] kv = strHeader.Split(":",2);
                        if (kv.Length >= 2)
                        {
                            if (!headers.ContainsKey(kv[0]))
                                headers.Add(kv[0], new[] { kv[1].Trim() });
                            else
                                headers[kv[0]] = headers[kv[0]].Append(kv[1].Trim()).ToArray();
                        }
                        else if(kv.Length == 1 && !headers.ContainsKey(kv[0]))
                            headers.Add(kv[0], Array.Empty<string>());

                    }
                }

                HttpStatus status = (GetResponse ?? DefaultResponse)
                    (operation, ((IPEndPoint?)e.LocalIp)?.Address, relUrl, headers, out byte[] body, out Dictionary<string, string[]> outHeaders);

                string response = status.GetFullHeader(outHeaders);

                _logger?.LogDebug("WebServer Log:\n" + response.Replace("\r\n", " >>\n"));

                byte[] outBuffer = Encoding.UTF8.GetBytes(response);
                cli.Send(outBuffer);
                cli.Send(body);
            }

            cli.Close();
        }
    }
}
