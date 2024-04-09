using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace FireLibs.Logging.Loggers
{
    /// <summary>
    /// A simple file logger service
    /// </summary>
    public class FileLogger : ILogger
    {
        private readonly FileLoggerConfiguration config;

        private readonly string fileName;
        private readonly Regex rgFilter;
        /// <summary>
        /// Creates an instance of <see cref="FileLogger"/>
        /// </summary>
        /// <param name="config">The configuration for the logger service</param>
        public FileLogger(FileLoggerConfiguration config)
        {
            this.config = config;
            rgFilter = new(@$"{config.FileIdentifier}(\d{{8}}-\d{{6}})");
            fileName = GetNewFileName();
            string repeated = "";
            int i = 0;
            if(!Directory.Exists(config.LogFolder))
                Directory.CreateDirectory(config.LogFolder);
            while (File.Exists(fileName + repeated))
                repeated = $" ({++i})";

            fileName += repeated + ".txt";
            File.Create(fileName).Close();
            DeleteOldFiles();
        }
        private string GetNewFileName() => Path.Combine(config.LogFolder,
            $"{config.FileIdentifier}{DateTime.Now:yyyyMMdd-HHmmss}");

        private void DeleteOldFiles()
        {
            if (config.MaxFiles <= 0)
                return;

            List<FileInfo> files = Directory.GetFiles(config.LogFolder, "*.*")
                .Where(f => rgFilter.IsMatch(Path.GetFileNameWithoutExtension(f)))
                .Select(f => new FileInfo(f)).OrderBy(f => f.CreationTime).ToList();

            int maxFiles = config.MaxFiles;
            while (files.Count > maxFiles)
            {
                files[0].Delete();
                files.RemoveAt(0);
            }
        }
        /// <inheritdoc/>
        public bool IsEnabled(LogLevel logLevel) => config.LogLevels.Contains(logLevel);
        /// <inheritdoc/>
        public void Log(LogEntry log)
        {
            if (!IsEnabled(log.LogLevel))
                return;

            File.AppendAllText(fileName, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] {log}\n");
        }
        /// <inheritdoc/>
        public void Dispose()
        {
            GC.SuppressFinalize(this);
        }
    }
    /// <summary>
    /// Configuration structure for <see cref="FileLogger"/>
    /// </summary>
    /// <param name="logLevels"><inheritdoc cref="LogLevels" path="/summary"/></param>
    /// <param name="fileName"><inheritdoc cref="FileIdentifier" path="/summary"/></param>
    /// <param name="folderName"><inheritdoc cref="LogFolder" path="/summary"/></param>
    /// <param name="maxFiles"><inheritdoc cref="MaxFiles" path="/summary"/></param>
    public struct FileLoggerConfiguration(LogLevel[] logLevels, string fileName = "log-", string folderName = "Logs", int maxFiles = 5)
    {
        /// <summary>
        /// A file indentifier for the <see cref="FileLogger"/>. The <see cref="FileLogger"/> will add the time and date of the creation concatenated after this string.
        /// </summary>
        public string FileIdentifier { get; private set; } = fileName;
        /// <summary>
        /// The folder where the log files will be created.
        /// </summary>
        public string LogFolder { get; private set; } = folderName;
        /// <summary>
        /// The maximum number of files (corresponding to <see cref="FileIdentifier"/> parameter) that will be retained.
        /// </summary>
        public int MaxFiles { get; private set; } = maxFiles;
        /// <summary>
        /// The log levels registered by the <see cref="FileLogger"/>
        /// </summary>
        public LogLevel[] LogLevels { get; private set; } = logLevels;
    }
    /// <summary>
    /// Extension methods for the <see cref="FileLogger"/>
    /// </summary>
    public static class FileLoggerExtensions
    {
        /// <summary>
        /// Adds a <see cref="FileLogger"/> service to the new <see cref="Logger"/>
        /// </summary>
        /// <param name="loggerBuilder">The builder that calls this method</param>
        /// <param name="config">The configuration for the logger service</param>
        public static LoggerBuilder AddFileLogger(this LoggerBuilder loggerBuilder, FileLoggerConfiguration config)
        {
            loggerBuilder.AddLogger(new FileLogger(config));
            return loggerBuilder;
        }
    }
}
