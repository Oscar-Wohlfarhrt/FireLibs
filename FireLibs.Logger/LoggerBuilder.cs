using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FireLibs.Logging
{
    /// <summary>
    /// Configure and create <see cref="Logger"/>s
    /// </summary>
    public class LoggerBuilder
    {
        private readonly List<ILogger> registeredLoggers = new();

        /// <summary>
        /// Register a <see cref="ILogger"/> for a new <see cref="Logger"/>
        /// </summary>
        /// <param name="logger">The <see cref="ILogger"/> to be added</param>
        public LoggerBuilder AddLogger(ILogger logger)
        {
            registeredLoggers.Add(logger);
            return this;
        }

        internal Logger CreateLogger()
        {
            return new Logger(registeredLoggers);
        }
    }
}
