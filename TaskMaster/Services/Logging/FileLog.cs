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

using System.Collections.Concurrent;
using System.IO;
using System.Text;

using TaskMaster.Application;

namespace TaskMaster.Services.Logging;

/// <summary>
/// File-based logger with log rotation.
/// </summary>
public sealed class FileLog : ILog
{
    private const int NewWriterMaxAttempts = 3;

    private readonly BlockingCollection<string> _queue = [.. new ConcurrentQueue<string>()];
    private readonly Thread _writerThread;

    private readonly LogLevel _minimumLevel;
    private readonly string _filePath;
    private readonly long _maxBytes;
    private readonly int _maxBackups;
    private readonly bool _autoClean;

    private volatile bool _disposed;

    private StreamWriter? _writer;


    /// <summary>
    /// Gets or sets the minimum log level.
    /// </summary>
    public LogLevel MinimumLevel
    {
        get { return _minimumLevel; }
    }


    /// <summary>
    /// Initializes a new instance of the <see cref="FileLog"/> class.
    /// </summary>
    /// <param name="level">The minimum logging level allow entries in the log.</param>
    /// <param name="filePath">The log file path.</param>
    /// <param name="maxBytes">The maximum size in bytes before rotation.</param>
    /// <param name="maxBackups">The maximum number of backup files to keep.</param>
    /// <param name="autoClean">A value indicating whether to auto-clean the log file when maxBackups is zero.</param>
    public FileLog(LogLevel level, string filePath, long maxBytes, int maxBackups, bool autoClean)
    {
        _minimumLevel = level;

        var appDir = Path.GetDirectoryName(AppInfo.Location) ?? AppDomain.CurrentDomain.BaseDirectory;
        _filePath = filePath ?? Path.Combine(appDir, "TaskMaster.log");
        _maxBytes = maxBytes <= 0 ? 1 * 1024 * 1024 : maxBytes;
        _maxBackups = Math.Max(0, maxBackups);
        _autoClean = autoClean;

        Directory.CreateDirectory(Path.GetDirectoryName(_filePath) ?? appDir);
        OpenWriter();

        _writerThread = new Thread(WriterLoop)
        {
            IsBackground = true,
            Name = "TaskMaster.FileLog"
        };

        _writerThread.Start();
    }


    /// <summary>
    /// Logs a trace-level message.
    /// </summary>
    /// <param name="message">The message to log.</param>
    public void Trace(string message) => Enqueue(LogLevel.Trace, message, null);

    /// <summary>
    /// Logs a debug-level message.
    /// </summary>
    /// <param name="message">The message to log.</param>
    public void Debug(string message) => Enqueue(LogLevel.Debug, message, null);

    /// <summary>
    /// Logs an info-level message.
    /// </summary>
    /// <param name="message">The message to log.</param>
    public void Info(string message) => Enqueue(LogLevel.Info, message, null);

    /// <summary>
    /// Logs a warning-level message.
    /// </summary>
    /// <param name="message">The message to log.</param>
    public void Warn(string message) => Enqueue(LogLevel.Warn, message, null);


    /// <summary>
    /// Logs an error-level message.
    /// </summary>
    /// <param name="message">The message to log.</param>
    public void Error(string message) => Enqueue(LogLevel.Error, message, null);

    /// <summary>
    /// Logs an error-level message with exception.
    /// </summary>
    /// <param name="message">The message to log.</param>
    /// <param name="ex">The exception to log.</param>
    public void Error(Exception ex, string message = "") => Enqueue(LogLevel.Error, message, ex);

    /// <summary>
    /// Logs a fatal-level message.
    /// </summary>
    /// <param name="message">The message to log.</param>
    public void Fatal(string message) => Enqueue(LogLevel.Fatal, message, null);

    /// <summary>
    /// Logs a fatal-level message with exception.
    /// </summary>
    /// <param name="message">The message to log.</param>
    /// <param name="ex">The exception to log.</param>
    public void Fatal(Exception ex, string message = "") => Enqueue(LogLevel.Fatal, message, ex);


    /// <summary>
    /// Flushes any buffered log messages to the file.
    /// </summary>
    public void Flush()
    {
        try
        {
            _writer?.Flush();
        }
        catch
        {
            // ...let it slide
        }
    }



    private void Enqueue(LogLevel level, string message, Exception? ex)
    {
        if (_disposed)
        {
            return;
        }

        if (level < MinimumLevel)
        {
            return;
        }

        var sb = new StringBuilder(256);
        sb.Append('[').Append(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff")).Append("] ");
        sb.Append('[').Append('T').Append(Environment.CurrentManagedThreadId).Append("] ");
        sb.Append('[').Append(level.ToString().ToUpperInvariant()).Append("] ");
        sb.Append(message ?? string.Empty);

        if (ex != null)
        {
            sb.AppendLine();
            sb.Append(ex.ToString());
        }

        _queue.TryAdd(sb.ToString());
    }

    private void WriterLoop()
    {
        try
        {
            foreach (var line in _queue.GetConsumingEnumerable())
            {
                WriteLine(line);
            }
        }
        catch
        {
            // As a last resort, do nothing -- logging must not crash the app
        }
    }

    private void WriteLine(string line)
    {
        if (_disposed)
        {
            return; // ...bail after disposal to avoid reopening
        }

        try
        {
            EnsureWriterAndRotateIfNeeded(line.Length + 2); // + CRLF
            _writer!.WriteLine(line);
        }
        catch
        {
            // Try to reopen once on failure
            try
            {
                OpenWriter();
                _writer?.WriteLine(line);
            }
            catch
            {
                // ...let it slide
            }
        }
    }

    private void EnsureWriterAndRotateIfNeeded(int incomingBytes)
    {
        if (_writer == null)
        {
            OpenWriter();
            return;
        }

        try
        {
            var len = (_writer.BaseStream?.Length) ?? 0L;
            if (len + incomingBytes <= _maxBytes)
            {
                return;
            }
        }
        catch
        {
            // ...let it slide -- fallsback to rotation attempt anyway
        }

        try
        {
            _writer.Flush();
            _writer.Dispose();
        }
        catch
        {
            // ...let it slide
        }

        RotateFiles();
        OpenWriter();
    }

    private void OpenWriter()
    {
        int attempt = 0;

        while (true)
        {
            try
            {
                var fs = new FileStream(
                    _filePath,
                    FileMode.Append,
                    FileAccess.Write,
                    FileShare.ReadWrite | FileShare.Delete);

                // Emit BOM for any non-ASCII that slips in (…, —, etc.)
                _writer = new StreamWriter(fs, new UTF8Encoding(encoderShouldEmitUTF8Identifier: true))
                {
                    AutoFlush = true
                };

                return;
            }
            catch (IOException) when (++attempt < NewWriterMaxAttempts)
            {
                // brief backoff for AV/Indexers/Editors holding a transient lock
                Thread.Sleep(50 * attempt);
            }
        }
    }

    private void RotateFiles()
    {
        try
        {
            // Delete oldest
            var fileExt = Path.GetExtension(_filePath);
            var filePathWithoutExt = _filePath.Replace(fileExt, string.Empty);
            var oldest = filePathWithoutExt + "." + _maxBackups + fileExt;
            if (_maxBackups > 0 && File.Exists(oldest))
            {
                File.Delete(oldest);
            }

            // Shift N-1..1 upward
            for (int i = _maxBackups - 1; i >= 1; i--)
            {
                var src = filePathWithoutExt + "." + i + fileExt;
                var dst = filePathWithoutExt + "." + (i + 1) + fileExt;
                if (File.Exists(src))
                {
                    File.Move(src, dst, overwrite: true);
                }
            }

            // Move current -> .1
            if (_maxBackups > 0 && File.Exists(_filePath))
            {
                File.Move(_filePath, filePathWithoutExt + ".1" + fileExt, overwrite: true);
            }

            if (_autoClean && _maxBackups == 0 && File.Exists(_filePath))
            {
                File.Delete(_filePath);
            }
        }
        catch
        {
            // ...let it slide
        }
    }


    /// <summary>
    /// Disposes the logger and releases resources.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _queue.CompleteAdding();
        try
        {
            _writerThread.Join(3000);
        }
        catch
        {
            // ...let it slide
        }

        try
        {
            _writer?.Flush();
            _writer?.Dispose();
        }
        catch
        {
            // ...let it slide
        }
    }
}
