//using FireLibs.Logging;
//using FireLibs.Logging.Loggers;
using FireLibs.Web.Http;
using System.IO;
using System.Net;
using System.Reflection.PortableExecutable;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using System.Linq;

namespace HttpServerTests
{
    internal class Program
    {
        static ILoggerFactory logFac = LoggerFactory.Create((builder) =>
            builder.SetMinimumLevel(LogLevel.Debug).AddProvider(new ToEventLoggerProvider()));
        static ILogger logger = logFac.CreateLogger<Program>();

        static WebSocket ws;
        static HttpServer server = new(5100, 5000, logFac.CreateLogger<HttpServer>());//, logger: consoleLogger

        static bool run = true;
        static DirectoryInfo? testPagesDir = Directory.GetParent(Environment.CurrentDirectory)?.Parent?.Parent?.GetDirectories().First(i => i.Name == "TestPages");
        static void Main(string[] args)
        {
            AppDomain.CurrentDomain.DomainUnload += ProcessExit;
            logger.LogInformation($"Using {testPagesDir?.FullName} directory for testing");

            server.OnProcessRequest = Server_ProcessRequest;
            ws = server.CreateOrGetWebSocket("/websocket/main");
            logger.LogInformation("Starting Server");

            server.Start();
            while (run) ;
        }

        private static void ProcessExit(object? sender, EventArgs e)
        {
            server.Stop();
        }

        static HttpResponse Server_ProcessRequest(HttpRequest req)
        {
            HttpResponse res = HttpResponse.NotImplementedResponse;
            if (req.IsOperation("GET") && req.Path == "/exit.exit")
            {
                res.SetContent("Shutdown server");
                res.Status = HttpDefaultStatus.OK;
                run = false;
            }
            else if (req.IsOperation("GET") && req.Path == "/")
            {
                string[] files = testPagesDir?.GetFiles("*.html", SearchOption.AllDirectories)?
                    .Select(f => $"/{Path.GetRelativePath(testPagesDir!.FullName, f.FullName)}").ToArray() ?? [];


                //res.SetContentFromFile("./test.html");
                res.SetContent(string.Join("\n", files.Select(f=> $"<a href='{f}'>{f}</a><br>\n")));
                res.Status = HttpDefaultStatus.OK;
            }
            else if (req.IsOperation("GET") && req.Path == "/websocket/mainurl")
            {
                //res.SetContentFromFile("./test.html");
                res.SetContent($"{{\"url\":\"ws://localhost:5100/websocket/main\"}}");
                res.Status = HttpDefaultStatus.OK;
            }
            else if (req.IsOperation("GET") && req.Path == "/websocket/maintest")
            {
                ws.SendText("This is a loooooooooong test dkajdslk salkdj salkdj salkd jsalkd jlsakjd lksaj dlksa jdlksa jdlksajdlkak lsljakdj salkdjalksdj salkjd lsadljsakjd lksa jdlksajd lksa jdsalk");
                res.SetContent("WebSocket Message Sended");
                res.Status = HttpDefaultStatus.OK;
            }
            else if (req.IsOperation("GET") && req.Path == "/websocket/mainrec")
            {
                string text = ws.ReadText();
                res.SetContent($"{ws.Available}: {text}"); // {ws.Available}:  string.Join(", ", ws.Read().Select(b => $"{b}"))
                res.Status = HttpDefaultStatus.OK;
            }
            else if (req.IsOperation("GET") && testPagesDir != null)
            {
                res.Status = HttpDefaultStatus.OK;
                FileInfo filePath = new(Path.Combine(testPagesDir.FullName, req.Path[1..]));
                if (filePath.Exists)
                {
                    res.SetContentFromFile(filePath.FullName);
                }
                else
                {
                    string[] headers = req.Headers.SelectMany(kv => kv.Value.Select(v => $"{kv.Key} = {v}")).ToArray();
                    res.SetContent($"=====[ File not found ]=====\nCurrent path is {req.Path} ({filePath.FullName})\nAnd the headers are:\n{string.Join("\n", headers)}");
                    res.Status = HttpDefaultStatus.NotFound;
                }
            }
            /*if (req.IsOperation("GET") && req.Path == "/events")
            {
                body = Encoding.UTF8.GetBytes("event: test\ndata: This is a test\n\n");
                outHeaders.Add("Content-Type", ["text/event-stream"]);
                outHeaders.Add("Cache-Control", ["no-cache"]);
                Thread.Sleep(1000);
                //outHeaders.Add("Content-length", [$"{body.Length}"]);
            }*/
            return res;
        }

        /*static HttpStatus GetResponse_Server(string op, IPAddress? ip, string path, Dictionary<string, string[]> inHeaders, string content, out byte[] body, out Dictionary<string, string[]> outHeaders)
        {
            //body = Encoding.UTF8.GetBytes($"{op} {path}");
            outHeaders = new();

            if (op == "GET" && path == "/exit.exit")
            {
                body = Encoding.UTF8.GetBytes("Shutdown server");
                run = false;
                return HttpStatus.OK;
            }
            if (op == "GET" && path == "/test")
            {
                body = Encoding.UTF8.GetBytes(File.ReadAllText("./test.html"));

                outHeaders.Add("Content-length", [$"{body.Length}"]);
                return HttpStatus.OK;
            }
            if (op == "GET" && path == "/events")
            {
                body = Encoding.UTF8.GetBytes("event: test\ndata: This is a test\n\n");
                outHeaders.Add("Content-Type", ["text/event-stream"]);
                outHeaders.Add("Cache-Control", ["no-cache"]);
                Thread.Sleep(1000);
                //outHeaders.Add("Content-length", [$"{body.Length}"]);
                return HttpStatus.OK;
            }

            string[] headers = inHeaders.SelectMany(kv => kv.Value.Select(v => $"{kv.Key} = {v}")).ToArray();
            body = Encoding.UTF8.GetBytes($"Current path is {path}\nAnd the headers are:\n{string.Join("\n", headers)}");

            outHeaders.Add("Content-length", [$"{body.Length}"]);
            return HttpStatus.OK;
        }*/
    }
}


public class ToEventLogger : ILogger
{
    private object _lock = new();
    private string Category { get; set; }
    public ToEventLogger(string category)
    {
        Category = category;
    }
    public IDisposable? BeginScope<TState>(TState state) where TState : notnull
    {
        return null;
    }

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        lock (_lock)
        {
            ConsoleColor prevColor = Console.ForegroundColor;
            ConsoleColor prevBack = Console.BackgroundColor;
            Console.ForegroundColor = logLevel switch {
                LogLevel.Information => ConsoleColor.DarkGreen,
                LogLevel.Warning => ConsoleColor.Yellow,
                LogLevel.Error => ConsoleColor.Red,
                LogLevel.Critical => ConsoleColor.DarkRed,
                _ => ConsoleColor.White,
            };
            Console.BackgroundColor = ConsoleColor.Black;
            Console.Write($"{logLevel}:");
            Console.ForegroundColor = prevColor;
            Console.BackgroundColor = prevBack;
            Console.WriteLine($" [{Category} ({eventId})] {formatter(state, exception)}");
        }
    }
}

public class ToEventLoggerProvider : ILoggerProvider
{
    public ILogger CreateLogger(string categoryName) => new ToEventLogger(categoryName);

    public void Dispose() { }
}