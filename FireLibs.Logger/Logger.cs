using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FireLibs.Logging
{
    /// <summary>
    /// The main logger class (and Thread-Safe) destinated to get and write all the logs.
    /// </summary>
    public class Logger : IDisposable
    {
        private readonly ConcurrentQueue<LogEntry> logQueue = [];

        /// <summary>
        /// List of logger services registered for this logger
        /// </summary>
        private readonly List<ILogger> loggers = [];

        private readonly Thread logThread;
        private readonly EventWaitHandle wh = new AutoResetEvent(true);
        private static readonly CancellationTokenSource tokenSource = new();
        private static CancellationToken cancellationToken;

        internal Logger(List<ILogger> loggerServices)
        {
            loggers = loggerServices;
            cancellationToken = tokenSource.Token;
            logThread = new Thread(LoggerThread);
            logThread.Start();
        }
        /// <summary>
        /// Adds a new <see cref="LogEntry"/> to be written
        /// </summary>
        /// <param name="log">The log entry struture to be written</param>
        public void Log(LogEntry log)
        {
            logQueue.Enqueue(log);
            wh.Set();
        }
        /// <summary>
        /// Adds a new <see cref="LogEntry"/> to be written
        /// </summary>
        /// <param name="level"><inheritdoc cref="LogEntry.LogLevel" path="/summary"/></param>
        /// <param name="message"><inheritdoc cref="LogEntry.Message" path="/summary"/></param>
        public void Log(LogLevel level, string message) => Log(new(level, message));
        /// <summary>
        /// Adds a new Trace <see cref="LogEntry"/> to be written
        /// </summary>
        /// <param name="message"><inheritdoc cref="LogEntry.Message" path="/summary"/></param>
        public void LogTrace(string message) => Log(LogLevel.Trace,message);
        /// <summary>
        /// Adds a new Debug <see cref="LogEntry"/> to be written
        /// </summary>
        /// <param name="message"><inheritdoc cref="LogEntry.Message" path="/summary"/></param>
        public void LogDebug(string message) => Log(LogLevel.Debug, message);
        /// <summary>
        /// Adds a new Information <see cref="LogEntry"/> to be written
        /// </summary>
        /// <param name="message"><inheritdoc cref="LogEntry.Message" path="/summary"/></param>
        public void LogInformation(string message) => Log(LogLevel.Information, message);
        /// <summary>
        /// Adds a new Warning <see cref="LogEntry"/> to be written
        /// </summary>
        /// <param name="message"><inheritdoc cref="LogEntry.Message" path="/summary"/></param>
        public void LogWarning(string message) => Log(LogLevel.Warning, message);
        /// <summary>
        /// Adds a new Error <see cref="LogEntry"/> to be written
        /// </summary>
        /// <param name="message"><inheritdoc cref="LogEntry.Message" path="/summary"/></param>
        public void LogError(string message) => Log(LogLevel.Error, message);
        /// <summary>
        /// Adds a new Critical <see cref="LogEntry"/> to be written
        /// </summary>
        /// <param name="message"><inheritdoc cref="LogEntry.Message" path="/summary"/></param>
        public void LogCritical(string message) => Log(LogLevel.Critical, message);

        /// <inheritdoc/>
        public void Dispose()
        {
            GC.SuppressFinalize(this);
            tokenSource.Cancel();
            wh.Set();
            logThread.Join();
        }
        /// <summary>
        /// Thread dedicated to write the logs to the logger services
        /// </summary>
        internal void LoggerThread()
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                if (!logQueue.IsEmpty)
                {
                    if(logQueue.TryDequeue(out LogEntry log))
                        foreach (ILogger logger in loggers)
                            logger.Log(log);
                }
                else
                {
                    wh.WaitOne();
                }
            }
        }
    }
}
