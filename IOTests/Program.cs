using FireLibs.Logging;
using FireLibs.Logging.Loggers;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

/* Program to test library functions and check it's functionalities */

//ILogger log = FileLoggerProvider.CreateLogger("Test");

//log.LogError("Test");

void OnLog_Event(LogEntry log)
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
Console.WriteLine("End");