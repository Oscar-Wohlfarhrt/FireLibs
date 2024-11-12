using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FireLibs.Logging.Loggers
{
    /// <summary>
    /// Log to the Visual Studio debug console.
    /// </summary>
    /// <remarks>
    /// Creates an instance of <see cref="VSConsoleLogger"/>
    /// </remarks>
    public class VSConsoleLogger : ILogger
    {
        private readonly LogLevel[] logLevels;

        /// <summary>
        /// Creates an instance of <see cref="VSConsoleLogger"/>
        /// </summary>
        /// <param name="logLevels">The log levels registered by the <see cref="VSConsoleLogger"/></param>
        public VSConsoleLogger(LogLevel[] logLevels)
        {
            this.logLevels = logLevels;
        }

        /// <inheritdoc/>
        public bool IsEnabled(LogLevel logLevel) => logLevels.Contains(logLevel);
        /// <inheritdoc/>
        public void Log(LogEntry log)
        {
            if (!IsEnabled(log.LogLevel))
                return;
            if(log.LogLevel == LogLevel.Trace)
                Trace.WriteLine(log.ToString());
            else
                Debug.WriteLine(log.ToString());
        }
        /// <inheritdoc/>
        public void Dispose()
        {
            GC.SuppressFinalize(this);
        }
    }

    /// <summary>
    /// Extension methods for the <see cref="VSConsoleLogger"/>
    /// </summary>
    public static class VSConsoleLoggerExtensions
    {
        /// <summary>
        /// Adds a <see cref="VSConsoleLogger"/> service to the new <see cref="Logger"/>
        /// </summary>
        /// <param name="loggerBuilder">The builder that calls this method</param>
        /// <param name="logLevels">The log levels registered by the <see cref="VSConsoleLogger"/></param>
        public static LoggerBuilder AddVSConsoleLogger(this LoggerBuilder loggerBuilder, LogLevel[] logLevels)
        {
            loggerBuilder.AddLogger(new VSConsoleLogger(logLevels));
            return loggerBuilder;
        }
    }
}
