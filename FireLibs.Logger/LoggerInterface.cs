using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FireLibs.Logging
{
    /// <summary>
    /// Represents all the information of a log entry
    /// </summary>
    /// <param name="logLevel">Level at which the log will be written</param>
    /// <param name="message">Log message to be written</param>
    public struct LogEntry(LogLevel logLevel, string message)
    {
        /// <summary>
        /// Level at which the log will be written
        /// </summary>
        public LogLevel LogLevel { get; set; } = logLevel;
        /// <summary>
        /// Log message to be written
        /// </summary>
        public string Message { get; set; } = message;

        /// <inheritdoc/>
        public override readonly string ToString() => $"{LogLevel}: {Message}";
    }
    /// <summary>
    /// Represents a logger service to be handled by a <see cref="Logger"/>
    /// </summary>
    public interface ILogger : IDisposable
    {
        /// <summary>
        /// Writes a log entry
        /// </summary>
        /// <param name="log">Log entry to be written</param>
        public void Log(LogEntry log);
        /// <summary>
        /// Checks if the given <paramref name="logLevel"/> is enabled
        /// </summary>
        /// <param name="logLevel">Level to be checked</param>
        public bool IsEnabled(LogLevel logLevel);
    }
}
