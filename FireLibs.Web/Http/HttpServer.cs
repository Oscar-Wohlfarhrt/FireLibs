using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;

using FireLibs.Web.TCP;
using System.Net.Security;
using System.ComponentModel.Design;
using System.Security.Cryptography;
using System;
using System.Linq;

namespace FireLibs.Web.Http
{
    public class HttpServer
    {
        internal static readonly Regex ReqMatcher = new(@"^(.+?) (.+?) (HTTP\/.+?)\r\n((?:.*?\r\n)*?)?\r\n(.*)", RegexOptions.Singleline);
        internal const string WebSocketGUID = "258EAFA5-E914-47DA-95CA-C5AB0DC85B11";


        SocketServer server;
        public int RequestTimeout { get; set; }

        public delegate HttpResponse ProcessRequest(HttpRequest request);
        public delegate HttpResponse MappedRequest(HttpRequest request, params string[] pathOptions);

        public delegate HttpStatus GetAction(string op, IPAddress? ip, string path, Dictionary<string, string[]> inHeaders, string content, out byte[] body, out Dictionary<string, List<string>> outHeaders);
        public GetAction? GetResponse { get; set; }
        public ProcessRequest? OnProcessRequest { get; set; }
        private ILogger? _logger;

        Dictionary<string, WebSocket> webSockets;
        Dictionary<string, MappedRequest> mappedOperations;

        public HttpServer(int port, int timeout = 1000, ILogger? logger = null)
            : this(new IPEndPoint(IPAddress.Any, port), timeout, logger) { }
        public HttpServer(IPAddress ip, int port, int timeout = 1000, ILogger? logger = null)
            : this(new IPEndPoint(ip,port),timeout,logger) { }
        public HttpServer(IPEndPoint endPoint, int timeout = 1000, ILogger? logger = null)
        {
            webSockets = new();
            mappedOperations = new();
            server = new(SocketType.Stream, ProtocolType.Tcp, endPoint);
            server.OnSocketConnected += ClientConnected;
            RequestTimeout = timeout;
            _logger = logger;
            _logger?.LogInformation($"Server created on: http://{(endPoint.Address.ToString() == "0.0.0.0" ? "localhost" : endPoint.Address)}:{endPoint.Port}");
        }
        public void Start() => server.Start();
        public void Stop() => server.Stop();

        private static HttpResponse DefaultResponseProcess(HttpRequest request) => new(HttpDefaultStatus.NotImplemented, new(), "");

        private void ClientConnected(object? sender, SocketConnectedEventArgs e)
        {
            //_logger?.LogTrace($"Socket Open {e.RemoteIp} =======================\n");
            Socket cli = e.Client;

            //for https: SslStream sslStream = new(new NetworkStream(cli),true,(s,cert,chain,sslPolicy)=>true,);
            bool isWS = false;
            while ( cli.ReceiveHttpRequest(RequestTimeout, out HttpRequest request))
            {
                if (isWS = AcceptWebSocket(request, ref cli))
                    break;

                _logger?.LogInformation($"{request.Operation} {request.Path} {request.Version}");
                //_logger?.LogTrace($"\n{request.Operation} {request.Path} {request.Version}\n{string.Join("\n", request.Headers.SelectMany(kv => kv.Value.Select(v => $"{kv.Key}: {v}")))}\n");
                string mapOp = $"{request.Operation}{request.Path}";
                KeyValuePair<string, MappedRequest> kv = mappedOperations.FirstOrDefault(pair => mapOp.StartsWith(pair.Key));
                HttpResponse response = kv.Key != null
                    ? kv.Value(request, mapOp[kv.Key.Length..].Split("/",StringSplitOptions.RemoveEmptyEntries))
                    : (OnProcessRequest ?? DefaultResponseProcess)(request);

                if (response.Status.Code == (int)HttpDefaultStatus.NotFound)
                    _logger?.LogWarning($"File not found");
                //_logger?.LogTrace($"\n{response.Status.GetFullHeader(response.Headers)}\n");
                cli.SendHttpResponse(response);
            }

            //_logger?.LogTrace($"Socket Closed {e.RemoteIp} =======================\n");
            if(!isWS)
                cli.Close();
        }
        public void MapOperation(string operation, string path, MappedRequest function)
        {
            string opPath = $"{operation}{path}";
            if (!mappedOperations.TryAdd(opPath, function))
                mappedOperations[opPath] = function;
        }
        public void MapGet(string path, MappedRequest function) => MapOperation("GET", path, function);
        public void MapPost(string path, MappedRequest function) => MapOperation("POST", path, function);
        public void MapPut(string path, MappedRequest function) => MapOperation("PUT", path, function);
        public void MapDelete(string path, MappedRequest function) => MapOperation("DELETE", path, function);
        public void MapGetToFile(string path, string filePath) => MapOperation("GET", path, (request, pathOptions) =>
        {
            HttpResponse response = HttpResponse.OkResponse;
            response.SetContentFromFile(filePath);
            return response;
        });
        private HttpResponse ProcessWebSocketRequest(HttpRequest request)
        {
            HttpResponse response = new(HttpDefaultStatus.SwitchingProtocols, new(),"");
            if (request.Headers.TryGetValue("Sec-WebSocket-Key", out string[]? value))
            {
                string wsAccept = Convert.ToBase64String(SHA1.HashData(Encoding.UTF8.GetBytes(value[0] + WebSocketGUID)));
                //response.SetContent($"{{\"event\": \"test\",\"data\": \"test\"}}");
                response.AddHeader("Connection", "Upgrade");
                response.AddHeader("Upgrade", "websocket");
                response.AddHeader("Sec-WebSocket-Accept", wsAccept);
            }
            else
                response.Status = HttpDefaultStatus.BadRequest;
            
            return response;
        }
        private bool AcceptWebSocket(HttpRequest request, ref Socket client)
        {
            if (webSockets.ContainsKey(request.Path))
            {
                _logger?.LogInformation($"WebSocket request on '{request.Path}' and headers:\n{request.Operation} {request.Path} {request.Version}\n{string.Join("\n", request.Headers.Select((kv) => $"{kv.Key}: {string.Join(", ", kv.Value)}"))}");
                HttpResponse response = ProcessWebSocketRequest(request);
                client.SendHttpResponse(response);

                if (response.Status.Code == (int)HttpStatusCode.SwitchingProtocols)
                {
                    webSockets[request.Path].Client = client;
                    _logger?.LogInformation($"WebSocket on '{request.Path}' connected");
                }
                return true;
            }
            return false;
        }

        public WebSocket CreateOrGetWebSocket(string uri)
        {
            if (!webSockets.ContainsKey(uri)) {
                WebSocket ws = new();
                webSockets.Add(uri, ws);
                _logger?.LogInformation($"WebSocket created on: {uri}");
                return ws;
            }
            return webSockets[uri];
        }
    }
}
