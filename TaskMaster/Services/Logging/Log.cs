/*
 This file is part of TaskMaster.
 Copyright (C) 2025 Aaron Semler

 TaskMaster is free software: you can redistribute it and/or modify
 it under the terms of the GNU General Public License as published by
 the Free Software Foundation, either version 3 of the License, or
 (at your option) any later version.

 This program is distributed in the hope that it will be useful,
 but WITHOUT ANY WARRANTY; without even the implied warranty of
 MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 GNU General Public License for more details.

 You should have received a copy of the GNU General Public License
 along with this program.  If not, see <http://www.gnu.org/licenses/>.
*/

using System.IO;

using TaskMaster.Application;
using TaskMaster.Model;

namespace TaskMaster.Services.Logging;

/// <summary>
/// Static logging manager.<br/>
/// <br/>
/// Log Levels:<br/>
/// - Trace: Very detailed logs, typically only useful for debugging specific issues.<br/>
/// - Debug: Detailed logs useful for diagnosing issues and understanding application flow.<br/>
/// - Info: General operational entries about application progress.<br/>
/// - Warn: Indications of potential issues or important events that are not errors.<br/>
/// - Error: Errors that prevent normal operation but do not crash the application.<br/>
/// - Fatal: Critical errors causing premature termination.<br/>
/// </summary>
internal static class Log
{
    private const string LogExtension = ".log";

    /// <summary>
    /// A no-operation logger that discards all log messages.
    /// </summary>
    private sealed class NoOpLog : ILog
    {
        /// <summary>
        /// Gets or sets the minimum log level. (Unused in NoOpLog)
        /// </summary>
        public LogLevel MinimumLevel { get; }

        /// <summary>
        /// Logs a trace-level message. (No operation)
        /// </summary>
        /// <param name="msg">The message to log.</param>
        public void Trace(string msg) { }

        /// <summary>
        /// Logs a debug-level message. (No operation)
        /// </summary>
        /// <param name="msg">The message to log.</param>
        public void Debug(string msg) { }

        /// <summary>
        /// Logs an info-level message. (No operation)
        /// </summary>
        /// <param name="msg">The message to log.</param>
        public void Info(string msg) { }

        /// <summary>
        /// Logs a warning-level message. (No operation)
        /// </summary>
        /// <param name="msg">The message to log.</param>
        public void Warn(string msg) { }

        /// <summary>
        /// Logs an error-level message. (No operation)
        /// </summary>
        /// <param name="msg">The message to log.</param>
        public void Error(string msg) { }

        /// <summary>
        /// Logs an error-level message with an exception. (No operation)
        /// </summary>
        /// <param name="ex">The exception to log.</param>
        /// <param name="msg">The message to log.</param>
        public void Error(Exception ex, string msg = "") { }

        /// <summary>
        /// Logs a fatal-level message. (No operation)
        /// </summary>
        /// <param name="msg">The message to log.</param>
        public void Fatal(string msg) { }

        /// <summary>
        /// Logs a fatal-level message with an exception. (No operation)
        /// </summary>
        /// <param name="ex">The exception to log.</param>
        /// <param name="msg">The message to log.</param>
        public void Fatal(Exception ex, string msg = "") { }

        /// <summary>
        /// Flushes any buffered log messages. (No operation)
        /// </summary>
        public void Flush() { }

        /// <summary>
        /// Disposes the logger. (No operation)
        /// </summary>
        public void Dispose() { }
    }


    private static readonly object Gate = new();

    private static ILog _current = new NoOpLog();


    /// <summary>
    /// Gets the current logger instance.
    /// </summary>
    public static ILog Current => _current;


    /// <summary>
    /// Initializes the logging system with the specified logger.
    /// </summary>
    /// <param name="logger">The logger instance to use.</param>
    public static void Initialize(ILog logger)
    {
        if (logger == null)
        {
            return;
        }

        lock (Gate)
        {
            var old = _current;
            _current = logger;
            (old as IDisposable)?.Dispose();
        }
    }
    /// <summary>
    /// Reconfigure logger from settings (enable/disable, path, rotation).
    /// </summary>
    /// <param name="s">The settings data.</param>
    public static void ReconfigureFrom(ISettingsData s)
    {
        if (s == null || !s.EnableLogging)
        {
            Replace(() => new NoOpLog());
            return;
        }

        var appDir = Path.GetDirectoryName(AppInfo.Location) ?? AppDomain.CurrentDomain.BaseDirectory;
        var path = string.IsNullOrWhiteSpace(s.LogFilePath)
            ? Path.Combine(appDir, AppInfo.Name + LogExtension)
            : s.LogFilePath;

        var minLevel = GetMinimumLogLevel(s.LoggingMinimumLevel);
        var maxBytes = Math.Max(1, s.MaxLogFileSizeMB) * 1024 * 1024;
        var backups = Math.Max(0, s.MaxLogBackupFiles);
        var cleanup = s.AutoCleanLogFile;

        Replace(() => new FileLog(minLevel, path, maxBytes, backups, cleanup));
    }



    // --- Pass-through ---

    /// <summary>
    /// Logs a trace-level message.
    /// </summary>
    /// <param name="msg">The message to log.</param>
    public static void Trace(string msg) => _current.Trace(msg);

    /// <summary>
    /// Logs a debug-level message.
    /// </summary>
    /// <param name="msg">The message to log.</param>
    public static void Debug(string msg) => _current.Debug(msg);

    /// <summary>
    /// Logs an info-level message.
    /// </summary>
    /// <param name="msg">The message to log.</param>
    public static void Info(string msg) => _current.Info(msg);

    /// <summary>
    /// Logs a warning-level message.
    /// </summary>
    /// <param name="msg">The message to log.</param>
    public static void Warn(string msg) => _current.Warn(msg);


    /// <summary>
    /// Logs an error-level message.
    /// </summary>
    /// <param name="msg">The message to log.</param>
    public static void Error(string msg) => _current.Error(msg);

    /// <summary>
    /// Logs an error-level message with an exception.
    /// </summary>
    /// <param name="ex">The exception to log.</param>
    /// <param name="msg">The message to log.</param>
    public static void Error(Exception ex, string msg = "") => _current.Error(ex, msg);


    /// <summary>
    /// Logs a fatal-level message.
    /// </summary>
    /// <param name="msg">The message to log.</param>
    public static void Fatal(string msg) => _current.Fatal(msg);

    /// <summary>
    /// Logs a fatal-level message with an exception.
    /// </summary>
    /// <param name="ex">The exception to log.</param>
    /// <param name="msg">The message to log.</param>
    public static void Fatal(Exception ex, string msg = "") => _current.Fatal(ex, msg);


    /// <summary>
    /// Flushes any buffered log messages.
    /// </summary>
    public static void Flush() => _current.Flush();



    /// <summary>
    /// Dispose the current logger first, then attempt to create the new one.<br/>
    /// If anything fails, fall back to NoOp so logging never crashes the app.
    /// </summary>
    /// <param name="factory">The logger factory function.</param>
    private static void Replace(Func<ILog> factory)
    {
        lock (Gate)
        {
            var old = _current;
            _current = new NoOpLog(); // safe during swap
            (old as IDisposable)?.Dispose();

            try
            {
                _current = factory();
            }
            catch
            {
                _current = new NoOpLog(); // never bubble
            }
        }
    }

    private static LogLevel GetMinimumLogLevel(int level)
    {
#if DEBUG
        // NOTE: In debug builds, default to the most verbose logging.
        level = 0;
#endif
        return Enum.IsDefined(typeof(LogLevel), level)
            ? (LogLevel)level
            : LogLevel.Error;
    }
}
