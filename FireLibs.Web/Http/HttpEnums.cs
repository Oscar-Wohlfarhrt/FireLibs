using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace FireLibs.Web.Http
{
    public enum HttpOperation
    {
        GET,
        HEAD,
        OPTIONS,
        TRACE,
        PUT,
        DELETE,
        POST,
        PATCH,
        CONNECT,
    }
    public enum HttpDefaultStatus
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
    public struct HttpStatus
    {
        public int Code { get; private set; }
        public string StatusMessage { get; private set; }
        public HttpStatus(int code, string statusMessage)
        {
            Code = code;
            StatusMessage = statusMessage;
        }
        public HttpStatus(HttpDefaultStatus status) :
            this((int)status, status.GetStatusPhrase()) { }

        public static implicit operator HttpStatus(HttpDefaultStatus status) => new(status);
    }

    public static class HttpHeaders
    {
        private const string eol = "\r\n";
        public static string GetFullHeader(this HttpStatus status, Dictionary<string, List<string>> extraHeaders, string httpVersion = "HTTP/1.1")
        {
            string[] headers = extraHeaders.SelectMany(kv => kv.Value.Select(v => $"{kv.Key}: {v}{eol}")).ToArray();
            return $"{httpVersion} {status.Code} {status.StatusMessage}{eol}{string.Join("", headers)}{eol}";
        }
        public static string GetStatusPhrase(this HttpDefaultStatus status) =>
            status.ToString().ToSentenceCase();
        private static string ToSentenceCase(this string str) =>
            Regex.Replace(str, "[a-z][A-Z]", m => $"{m.Value[0]} {m.Value[1]}");
    }
}
