using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace FireLibs.Web.Http
{
    public struct HttpRequest
    {
        private static readonly Regex ReqMatcher = new(@"^(.+?) (.+?) (HTTP\/.+?)\r\n((?:.*?\r\n)*?)?\r\n(.*)", RegexOptions.Singleline);

        public static HttpRequest EmptyRequest => new("", "", new(), "", null);

        public string Operation { get; private set; }
        public string Path { get; private set; }
        public Dictionary<string, string[]> Headers { get; private set; }
        public string Content { get; private set; }
        public IPEndPoint? RemoteEndPoint { get; private set; }
        public string Version { get; private set; }

        public HttpRequest(string operation,string path, Dictionary<string, string[]> headers, string content, IPEndPoint? remoteEndPoint, string version = "")
        {
            Operation = operation;
            Path = path;
            Headers = headers;
            Content = content;
            RemoteEndPoint = remoteEndPoint;
            Version = version;
        }
        public readonly bool IsOperation(string op) => Operation == op;
        public readonly bool IsOperation(HttpOperation op) => IsOperation(op.ToString());

        public static bool TryParseHttpRequest(string strRequest, out HttpRequest request, IPEndPoint? endPoint = null)
        {
            Match match = ReqMatcher.Match(strRequest);

            if (match.Success)
            {
                string operation = "", relUrl = "", httpVersion = "", content = "";
                Dictionary<string, string[]> headers = new();
                if (match.Groups.Count >= 3)
                {
                    operation = match.Groups[1].Value;
                    relUrl = match.Groups[2].Value;
                    httpVersion = match.Groups[3].Value;
                }
                if (match.Groups.Count >= 4)
                {
                    string[] strHeaders = match.Groups[4].Value.Split("\r\n", StringSplitOptions.RemoveEmptyEntries);
                    foreach (string strHeader in strHeaders)
                    {
                        string[] kv = strHeader.Split(":", 2);
                        if (kv.Length >= 2)
                        {
                            if (!headers.ContainsKey(kv[0]))
                                headers.Add(kv[0], new[] { kv[1].Trim() });
                            else
                                headers[kv[0]] = headers[kv[0]].Append(kv[1].Trim()).ToArray();
                        }
                        else if (kv.Length == 1 && !headers.ContainsKey(kv[0]))
                            headers.Add(kv[0], Array.Empty<string>());

                    }
                }
                if (match.Groups.Count >= 5)
                    content = match.Groups[5].Value;

                request = new(operation, relUrl, headers, content, endPoint, httpVersion);
                return true;
            }
            request = EmptyRequest;
            return false;
        }
    }

    public struct HttpResponse
    {
        private static readonly Regex HttpVerChecker = new(@"^HTTP\/.{1,3}$");
        public static HttpResponse NotImplementedResponse => new(HttpDefaultStatus.NotImplemented, new(), "");
        public static HttpResponse NotFoundResponse => new(HttpDefaultStatus.NotFound, new(),"");
        public static HttpResponse OkResponse => new(HttpDefaultStatus.OK, new(), "");

        public HttpStatus Status { get; set; }
        public Dictionary<string, List<string>> Headers { get; private set; }
        public string Content { get; private set; }
        public string Version { get; private set; }

        public HttpResponse(HttpStatus status, Dictionary<string, List<string>> headers, string content = "", string type = "", string version = "HTTP/1.1")
        {
            Status = status;
            Headers = headers;
            SetContent(content, type);
            if (HttpVerChecker.IsMatch(version))
                Version = version;
            else
                Version = "HTTP/1.1";

        }

        public void AddHeader(string key, string value)
        {
            if (Headers.ContainsKey(key))
                Headers[key].Add(value);
            else
                Headers.Add(key, new() { value });
        }
        public void SetHeader(string key, List<string> value)
        {
            if (Headers.ContainsKey(key))
                Headers[key] = value;
            else
                Headers.Add(key, value);
        }
        public void SetContent(string content, string type="")
        {
            Content = content;
            SetContentLenght();
            if (type != "")
                SetContentType(type);
        }
        public void SetContentType(string type) => SetHeader("Content-Type", new() { type });

        public void SetContentFromFile(string path) => SetContent(File.ReadAllText(path));
        public void SetContentLenght() => SetHeader("Content-length", new(){ $"{Content.Length}" });
    }
}
