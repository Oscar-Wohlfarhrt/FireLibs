using FireLibs.Logging;
using FireLibs.Logging.Loggers;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using FireLibs.Web.Http;
using System.Net;
using System.IO;
using System.Text;

/* Program to test library functions and check it's functionalities */

//ILogger log = FileLoggerProvider.CreateLogger("Test");

//log.LogError("Test");

/*void OnLog_Event(LogEntry log)
{
    Console.WriteLine(log.ToString());
}

EventLoggerConfiguration elConfig = new([LogLevel.Information, LogLevel.Warning, LogLevel.Error], [OnLog_Event]);
FileLoggerConfiguration flConfig = new([LogLevel.Information, LogLevel.Warning, LogLevel.Error]);
var logger = LoggerFactory.GetOrCreateLogger("Logger1",(builder)=>
    builder.AddFileLogger(flConfig).AddEventLogger(elConfig).AddVSConsoleLogger([LogLevel.Trace,LogLevel.Debug]));

logger.LogTrace("This is trace log");
logger.LogDebug("This is debug log");
logger.LogInformation("This is information");
logger.LogWarning("This is a warning!!!");

Console.ReadKey();
Console.WriteLine("Disposing...");
LoggerFactory.RemoveLogger("Logger1");
Console.WriteLine("End");*/

void Logger_OnLog(LogEntry log) => Console.WriteLine(log);
Logger consoleLogger = LoggerFactory.GetOrCreateLogger("ConsoleLogger",
    (builder) => builder.AddEventLogger(new(
        [LogLevel.Debug, LogLevel.Information, LogLevel.Warning, LogLevel.Error, LogLevel.Critical],
        [Logger_OnLog]))
    );

bool run = true;
HttpServer server = new(5000, logger: consoleLogger);
server.GetResponse=GetResponse_Server;

server.Start();

HttpStatus GetResponse_Server(string op, IPAddress? ip, string path, Dictionary<string, string[]> inHeaders, out byte[] body, out Dictionary<string, string[]> outHeaders)
{
    body = Encoding.UTF8.GetBytes($"{op} {path}");
    outHeaders = new();

    if(op == "GET" && path == "/exit.exit")
    {
        body = Encoding.UTF8.GetBytes("Shutdown server");
        run = false;
        server.Stop();
        return HttpStatus.OK;
    }
    if (op == "GET" && path != "/notfoundtest")
    {
        string[] headers = inHeaders.SelectMany(kv => kv.Value.Select(v => $"{kv.Key} = {v}")).ToArray();
        body = Encoding.UTF8.GetBytes($"Current path is {path}\nAnd the headers are:\n{string.Join("\n",headers)}");
        return HttpStatus.OK;
    }

    return HttpStatus.OK;
}


while (run) ;
LoggerFactory.RemoveAllLoggers();
//server.Stop();
Console.WriteLine("Server shutdown");
