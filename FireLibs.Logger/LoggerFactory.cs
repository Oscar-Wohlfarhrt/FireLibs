using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FireLibs.Logging
{
    /// <summary>
    /// Defines logging severity levels.
    /// </summary>
    public enum LogLevel
    {
        /// <summary>
        /// Logs that contain the most detailed messages. These messages may contain sensitive application data.
        /// These messages are disabled by default and should never be enabled in a production environment.
        /// </summary>
        Trace,
        /// <summary>
        /// Logs that are used for interactive investigation during development.  These logs should primarily contain
        /// information useful for debugging and have no long-term value.
        /// </summary>
        Debug,
        /// <summary>
        /// Logs that track the general flow of the application. These logs should have long-term value.
        /// </summary>
        Information,
        /// <summary>
        /// Logs that highlight an abnormal or unexpected event in the application flow, but do not otherwise cause the
        /// application execution to stop.
        /// </summary>
        Warning,
        /// <summary>
        /// Logs that highlight when the current flow of execution is stopped due to a failure. These should indicate a
        /// failure in the current activity, not an application-wide failure.
        /// </summary>
        Error,
        /// <summary>
        /// Logs that describe an unrecoverable application or system crash, or a catastrophic failure that requires
        /// immediate attention.
        /// </summary>
        Critical,
        /// <summary>
        /// Not used for writing log messages. Specifies that a logging category should not write any messages.
        /// </summary>
        None
    }

    /// <summary>
    /// Creates new <see cref="Logger"/>s and store the created ones to be requested lately.
    /// </summary>
    public static class LoggerFactory
    {
        private static readonly Dictionary<string, Logger> loggers = new();

        private static Logger CreateLogger(string loggerName,LoggerBuilder builder)
        {
            Logger logger = builder.CreateLogger();
            loggers.TryAdd(loggerName, logger);
            return logger;
        }
        /// <summary>
        /// Gets the logger identified by the <paramref name="loggerName"/> if is already created, otherwise creates one.
        /// </summary>
        /// <param name="loggerName">The id for the logger</param>
        /// <param name="builderFunc">The function that configures the logger</param>
        /// <returns>A logger with the configuration defined by the <paramref name="builderFunc"/></returns>
        public static Logger GetOrCreateLogger(string loggerName, Func<LoggerBuilder, LoggerBuilder> builderFunc) =>
            GetOrCreateLogger(loggerName, builderFunc(new LoggerBuilder()));
        /// <summary>
        /// Gets the logger identified by the <paramref name="loggerName"/> if is already created, otherwise creates one.
        /// </summary>
        /// <param name="loggerName">The id for the logger</param>
        /// <param name="builder">The logger builder that configures the logger</param>
        /// <returns>A logger with the configuration defined by the <paramref name="builder"/></returns>
        public static Logger GetOrCreateLogger(string loggerName, LoggerBuilder builder)
        {
            if (loggers.TryGetValue(loggerName, out Logger? value))
                return value;
            return CreateLogger(loggerName,builder);
        }
        /// <summary>
        /// Removes an existing logger.
        /// </summary>
        /// <param name="loggerName">The logger to be removed</param>
        public static void RemoveLogger(string loggerName)
        {
            if (loggers.TryGetValue(loggerName, out Logger? value))
            {
                value.Dispose();
                loggers.Remove(loggerName);
            }
        }
        /// <summary>
        /// Removes all existing loggers. Intended for calling on program end.
        /// </summary>
        public static void RemoveAllLoggers() => loggers.Clear();
    }
}
