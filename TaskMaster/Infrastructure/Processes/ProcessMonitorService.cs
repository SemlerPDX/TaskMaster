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

using System.Diagnostics;

using TaskMaster.Services.Logging;

using Timer = System.Threading.Timer;

namespace TaskMaster.Infrastructure.Processes;

/// <summary>
/// Service that monitors running processes on the system.
/// </summary>
public sealed class ProcessMonitorService : IProcessMonitorService, IDisposable
{
    /// <summary>
    /// Information about a running process.
    /// </summary>
    public sealed class ProcessEntry
    {
        /// <summary>
        /// The bare process name, without ".exe".
        /// </summary>
        public string Name { get; set; } = string.Empty; // i.e. "VoiceMeeter"

        /// <summary>
        /// Set of full paths where this process is running from.
        /// </summary>
        public HashSet<string> Paths { get; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// List of PIDs for this process name.
        /// </summary>
        public List<int> Pids { get; } = [];
    }


    /// <summary>
    /// Event fired whenever the list of running processes is updated.
    /// </summary>
    public event Action? ProcessesUpdated;


    private readonly HashSet<string> _lastNameSet = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, ProcessEntry> _map = new(StringComparer.OrdinalIgnoreCase);

    private readonly Timer _timer;
    private readonly object _lock = new();

    private volatile bool _paused;

    private bool _disposed;


    /// <summary>
    /// Create a new ProcessMonitorService that polls every N 'seconds'.
    /// </summary>
    /// <param name="seconds">The polling interval in seconds.</param>
    public ProcessMonitorService(double seconds)
    {
        var interval = GetTimeSpan(seconds);
        _timer = new Timer(OnTimerTick, null, TimeSpan.Zero, interval);
    }


    /// <summary>
    /// Pause monitoring temporarily.
    /// </summary>
    public void Pause()
    {
        _paused = true;
    }

    /// <summary>
    /// Resume monitoring after a pause.
    /// </summary>
    public void Resume()
    {
        _paused = false;
    }

    /// <summary>
    /// Update the polling interval of the active timer.
    /// </summary>
    /// <param name="seconds">The new interval in seconds.</param>
    public void UpdateInterval(double seconds)
    {
        if (_disposed)
        {
            return;
        }

        try
        {
            var interval = GetTimeSpan(seconds);
            _timer.Change(TimeSpan.Zero, interval);
        }
        catch
        {
            // ...let it slide; best-effort change
            Log.Trace($"Failed to update ProcessMonitorService interval to {seconds} seconds.");
        }
    }

    /// <summary>
    /// Get a snapshot of the currently running processes.
    /// </summary>
    /// <returns>A read-only dictionary mapping process names (without .exe) to ProcessEntry objects.</returns>
    public IReadOnlyDictionary<string, ProcessEntry> Snapshot()
    {
        lock (_lock)
        {
            // Deep-ish copy so callers can read safely
            var copy = new Dictionary<string, ProcessEntry>(_map.Comparer);
            foreach (var kv in _map)
            {
                var pe = new ProcessEntry
                {
                    Name = kv.Value.Name
                };

                pe.Paths.UnionWith(kv.Value.Paths);
                pe.Pids.AddRange(kv.Value.Pids);
                copy[kv.Key] = pe;
            }

            return copy;
        }
    }

    /// <summary>
    /// Get the paths of all instances of a given process name from the latest snapshot.
    /// </summary>
    /// <param name="processName">The bare process name, without ".exe". Case insensitive.</param>
    /// <returns>An array of paths for all instances of the specified process name.</returns>
    public string[] GetPathsFromSnapshot(string processName)
    {
        lock (_lock)
        {
            if (_map.TryGetValue(processName, out var entry))
            {
                return entry.Paths.Count > 0 ? [.. entry.Paths] : [];
            }

            return [];
        }
    }

    /// <summary>
    /// Check if a process with the given name (without .exe) is currently running.
    /// </summary>
    /// <param name="processName">The bare process name, without ".exe". Case insensitive.</param>
    /// <returns>True if running, false otherwise.</returns>
    public bool IsRunning(string processName)
    {
        lock (_lock)
        {
            return _map.ContainsKey(processName);
        }
    }

    /// <summary>
    /// Kill all processes with the given name (without .exe).
    /// </summary>
    /// <param name="processName">The bare process name, without ".exe". Case insensitive.</param>
    /// <returns>The number of processes killed.</returns>
    public static int KillAll(string processName)
    {
        int count = 0;
        foreach (var p in Process.GetProcessesByName(processName))
        {
            try
            {
                p.Kill();
                count++;
            }
            catch (Exception ex)
            {
                Log.Debug($"Failed to kill process '{processName}' - Reason: {ex}");
            }
        }

        return count;
    }


    private void OnTimerTick(object? state)
    {
        if (_paused || _disposed)
        {
            return;
        }

        // Build next snapshot (names + PIDs)
        var next = new Dictionary<string, ProcessEntry>(StringComparer.OrdinalIgnoreCase);
        var processes = Process.GetProcesses();

        foreach (var p in processes)
        {
            string name = p.ProcessName; // bare name, no ".exe"

            if (!next.TryGetValue(name, out var entry))
            {
                entry = new ProcessEntry { Name = name };
                next[name] = entry;
            }

            entry.Pids.Add(p.Id);
        }

        bool changed;
        lock (_lock)
        {
            // Compare hashsets of names only
            if (_lastNameSet.Count == next.Count && _lastNameSet.SetEquals(next.Keys))
            {
                // Names identical -> refresh internal map but DO NOT notify
                changed = false;
            }
            else
            {
                // Names differ -> update internal map and cached key set, then notify
                SortMapLexicographically(next);

                _lastNameSet.Clear();
                _lastNameSet.UnionWith(_map.Keys);

                changed = true;
            }
        }

        if (changed)
        {
            ProcessesUpdated?.Invoke();
        }
    }

    private void SortMapLexicographically(Dictionary<string, ProcessEntry> map)
    {
        _map.Clear();
        var sorted = new SortedDictionary<string, ProcessEntry>(map, StringComparer.OrdinalIgnoreCase);
        foreach (var kv in sorted)
        {
            _map[kv.Key] = kv.Value;
        }
    }

    private static TimeSpan GetTimeSpan(double? seconds)
    {
        // Ensure 0.10 seconds is the minimum allowed interval for stability.
        if (seconds.HasValue && seconds.Value < 0.1D)
        {
            return TimeSpan.FromSeconds(0.1D);
        }

        // Fallback to 1 second.
        return TimeSpan.FromSeconds(seconds ?? 1D);
    }


    /// <summary>
    /// Dispose the service and stop monitoring.
    /// </summary>
    public void Dispose()
    {
        _disposed = true;
        _timer.Dispose();
    }
}
