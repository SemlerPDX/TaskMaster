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
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Windows.Threading;

namespace TaskMaster.Presentation.UI;

/// <summary>
/// A service to maintain a history log of timestamped messages.
/// </summary>
internal sealed class HistoryLog : IHistoryLog, IDisposable
{
    /// <summary>
    /// Maximum number of entries to drain from the queue per flush operation.
    /// </summary>
    public const int MaxDrainPerFlush = 64;


    private readonly ObservableCollection<string> _items;
    private readonly ReadOnlyObservableCollection<string> _readonlyItems;
    private readonly ConcurrentQueue<string> _queue;
    private readonly Dispatcher? _ui;

    /// <summary>
    /// Indicates whether a flush operation is scheduled on the UI thread.<br/>
    /// 0 = no flush scheduled, 1 = flush scheduled
    /// </summary>
    private int _flushScheduled;


    /// <summary>
    /// Gets a read-only collection of history log entries.
    /// </summary>
    public ReadOnlyObservableCollection<string> Items
    {
        get { return _readonlyItems; }
    }

    /// <summary>
    /// Maximum number of entries kept in the visible log.
    /// </summary>
    public int MaxEntries { get; set; } = 1000;



    /// <summary>
    /// Initializes a new instance of the <see cref="HistoryLog"/> class.
    /// </summary>
    public HistoryLog()
    {
        _items = new ObservableCollection<string>();
        _readonlyItems = new ReadOnlyObservableCollection<string>(_items);
        _queue = new ConcurrentQueue<string>();
        _ui = System.Windows.Application.Current?.Dispatcher;

        _items.CollectionChanged += OnCollectionChanged;
    }



    /// <summary>
    /// Appends a new entry to the history log with a timestamp.
    /// </summary>
    /// <param name="message">The message to log.</param>
    public void Append(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return;
        }

        string line = $"[{DateTime.Now:HH:mm:ss}]  {message}";

        // Design-time or already on UI thread
        if (_ui == null || _ui.CheckAccess())
        {
            _items.Add(line);
            return;
        }

        // Background thread: enqueue and schedule a flush onto the UI thread
        _queue.Enqueue(line);
        ScheduleFlush();
    }

    /// <summary>
    /// Clears all history log entries.
    /// </summary>
    public void Clear()
    {
        // Drain the queue
        while (_queue.TryDequeue(out _))
        {
        }

        if (_ui == null || _ui.CheckAccess())
        {
            _items.Clear();
        }
        else
        {
            _ui.BeginInvoke(
                new Action(() => _items.Clear()),
                DispatcherPriority.Background);
        }
    }

    /// <summary>
    /// Removes the last entry from the history log.
    /// </summary>
    public void ClearLastItem()
    {
        if (_ui == null || _ui.CheckAccess())
        {
            if (_items.Count > 0)
            {
                _items.RemoveAt(_items.Count - 1);
            }
        }
        else
        {
            _ui.BeginInvoke(
                new Action(
                    () =>
                    {
                        if (_items.Count > 0)
                        {
                            _items.RemoveAt(_items.Count - 1);
                        }
                    }),
                DispatcherPriority.Background);
        }
    }


    private void ScheduleFlush()
    {
        if (_ui == null)
        {
            // No dispatcher (design-time only) - just flush synchronously....
            FlushOnce();
            return;
        }

        // Coalesce multiple requests into a single scheduled flush
        if (Interlocked.Exchange(ref _flushScheduled, 1) == 1)
        {
            return;
        }

        _ui.BeginInvoke(
            new Action(FlushOnce),
            DispatcherPriority.Background);
    }

    private void FlushOnce()
    {
        try
        {
            if (_ui != null && !_ui.CheckAccess())
            {
                // Somehow not on UI thread; reschedule...
                ScheduleFlush();
                return;
            }

            var drained = 0;

            while (drained < MaxDrainPerFlush && _queue.TryDequeue(out var line))
            {
                _items.Add(line);
                drained++;
            }
        }
        finally
        {
            Interlocked.Exchange(ref _flushScheduled, 0);

            // If more work arrived while flushing, schedule another pass
            if (!_queue.IsEmpty)
            {
                ScheduleFlush();
            }
        }
    }

    private void OnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        // Guard against nonsense values; fall back to something sane
        if (MaxEntries <= 0)
        {
            MaxEntries = 100;
        }

        while (_items.Count > MaxEntries)
        {
            _items.RemoveAt(0);
        }
    }


    /// <summary>
    /// Disposes the history log and releases resources.
    /// </summary>
    public void Dispose()
    {
        _items.CollectionChanged -= OnCollectionChanged;

        while (_queue.TryDequeue(out _))
        {
        }
    }
}
