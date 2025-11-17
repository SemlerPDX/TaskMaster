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

namespace TaskMaster.Services.Logging;

/// <summary>
/// Defines logging severity levels.<br/>
/// <br/>
/// Log Levels:<br/>
/// [0] Trace: Very detailed logs, typically only useful for debugging specific issues.<br/>
/// [1] Debug: Detailed logs useful for diagnosing issues and understanding application flow.<br/>
/// [2] Info: General operational entries about application progress.<br/>
/// [3] Warn: Indications of potential issues or important events that are not errors.<br/>
/// [4] Error: Errors that prevent normal operation but do not crash the application.<br/>
/// [5] Fatal: Critical errors causing premature termination.<br/>
/// </summary>
public enum LogLevel
{
    /// <summary>
    /// Very detailed logs, typically only useful for debugging specific issues.
    /// </summary>
    Trace = 0,

    /// <summary>
    /// Detailed logs useful for diagnosing issues and understanding application flow.
    /// </summary>
    Debug = 1,

    /// <summary>
    /// General operational entries about application progress.
    /// </summary>
    Info = 2,

    /// <summary>
    /// Indications of potential issues or important events that are not errors.
    /// </summary>
    Warn = 3,

    /// <summary>
    /// Errors that prevent normal operation but do not crash the application.
    /// </summary>
    Error = 4,

    /// <summary>
    /// Critical errors causing premature termination.
    /// </summary>
    Fatal = 5
}

/// <summary>
/// Static logging manager interface.<br/>
/// <br/>
/// Log Levels:<br/>
/// - Trace: Very detailed logs, typically only useful for debugging specific issues.<br/>
/// - Debug: Detailed logs useful for diagnosing issues and understanding application flow.<br/>
/// - Info: General operational entries about application progress.<br/>
/// - Warn: Indications of potential issues or important events that are not errors.<br/>
/// - Error: Errors that prevent normal operation but do not crash the application.<br/>
/// - Fatal: Critical errors causing premature termination.<br/>
/// </summary>
public interface ILog : IDisposable
{
    /// <summary>
    /// Gets or sets the minimum log level.
    /// </summary>
    LogLevel MinimumLevel { get; }

    /// <summary>
    /// Logs a trace-level message.
    /// </summary>
    /// <param name="message">The message to log.</param>
    void Trace(string message);

    /// <summary>
    /// Logs a debug-level message.
    /// </summary>
    /// <param name="message">The message to log.</param>
    void Debug(string message);

    /// <summary>
    /// Logs an info-level message.
    /// </summary>
    /// <param name="message">The message to log.</param>
    void Info(string message);

    /// <summary>
    /// Logs a warning-level message.
    /// </summary>
    /// <param name="message">The message to log.</param>
    void Warn(string message);


    /// <summary>
    /// Logs an error-level message.
    /// </summary>
    /// <param name="message">The message to log.</param>
    void Error(string message);

    /// <summary>
    /// Logs an error-level message with an exception.
    /// </summary>
    /// <param name="ex">The exception to log.</param>
    /// <param name="message">The message to log.</param>
    void Error(Exception ex, string message = "");


    /// <summary>
    /// Logs a fatal-level message.
    /// </summary>
    /// <param name="message">The message to log.</param>
    void Fatal(string message);

    /// <summary>
    /// Logs a fatal-level message with an exception.
    /// </summary>
    /// <param name="ex">The exception to log.</param>
    /// <param name="message">The message to log.</param>
    void Fatal(Exception ex, string message = "");


    /// <summary>
    /// Flushes any buffered log messages.
    /// </summary>
    void Flush();
}
