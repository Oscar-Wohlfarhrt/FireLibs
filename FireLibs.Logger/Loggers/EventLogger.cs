using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FireLibs.Logging.Loggers
{
    /// <summary>
    /// Log to an event, which is called when a log is writen
    /// </summary>
    public class EventLogger : ILogger
    {
        private readonly LogLevel[] logLevels;

        private event EventLoggerDelegate? OnLog = null;

        /// <summary>
        /// Creates an instance of <see cref="EventLogger"/>
        /// </summary>
        /// <param name="config">The configuration for the logger service</param>
        public EventLogger(EventLoggerConfiguration config)
        {
            foreach(var deleg in config.Delegates)
                OnLog += deleg;
            logLevels=config.LogLevels;
        }

        /// <inheritdoc/>
        public bool IsEnabled(LogLevel logLevel) => logLevels.Contains(logLevel);
        /// <inheritdoc/>
        public void Log(LogEntry log)
        {
            if (!IsEnabled(log.LogLevel))
                return;
            OnLog?.Invoke(log);
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            GC.SuppressFinalize(this);
        }
    }
    /// <summary>
    /// Delegate for the <see cref="EventLogger"/> events
    /// </summary>
    /// <param name="log">A log entry</param>
    public delegate void EventLoggerDelegate(LogEntry log);

    /// <summary>
    /// Configuration structure for <see cref="EventLogger"/>
    /// </summary>
    public struct EventLoggerConfiguration
    {
        /// <summary>
        /// An array of the functions suscribed to the <see cref="EventLogger"/>
        /// </summary>
        public EventLoggerDelegate[] Delegates { get; private set; }
        /// <summary>
        /// The log levels registered by the <see cref="EventLogger"/>
        /// </summary>
        public LogLevel[] LogLevels { get; private set; }

        /// <summary>
        /// Creates an instance of <see cref="EventLoggerConfiguration"/>
        /// </summary>
        /// <param name="logLevels"><inheritdoc cref="LogLevels" path="/summary"/></param>
        /// <param name="delegates"><inheritdoc cref="Delegates" path="/summary"/></param>
        public EventLoggerConfiguration(LogLevel[] logLevels, EventLoggerDelegate[] delegates)
        {
            Delegates = delegates;
            LogLevels = logLevels;
        }
    }
    /// <summary>
    /// Extension methods for the <see cref="EventLogger"/>
    /// </summary>
    public static class EventLoggerExtensions
    {
        /// <summary>
        /// Adds a <see cref="EventLogger"/> service to the new <see cref="Logger"/>
        /// </summary>
        /// <param name="loggerBuilder">The builder that calls this method</param>
        /// <param name="config">The configuration for the logger service</param>
        public static LoggerBuilder AddEventLogger(this LoggerBuilder loggerBuilder, EventLoggerConfiguration config)
        {
            loggerBuilder.AddLogger(new EventLogger(config));
            return loggerBuilder;
        }
    }
}
