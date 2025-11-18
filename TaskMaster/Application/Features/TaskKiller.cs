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

using TaskMaster.Application.Composition;
using TaskMaster.Infrastructure.Processes;
using TaskMaster.Services;
using TaskMaster.Services.Logging;

namespace TaskMaster.Application.Features;

/// <summary>
/// Background service that terminates configured processes on a periodic interval,
/// honoring an optional grace delay before killing.<br/>
/// Also manages retry attempts and max retry limits per settings.
/// </summary>
public sealed class TaskKiller : ITaskKiller, IDisposable
{
    private const string DetectedMessageKey = "Message.KillerTab.History.Detected";

    private const string KilledMessageKey = "Message.KillerTab.History.Killed";
    private const string KilledTrayTitleKey = "Title.TaskKiller.Tray.Killed";
    private const string KilledTrayMessageKey = "Message.TaskKiller.Tray.Killed";

    private const string DeniedMessageKey = "Message.KillerTab.History.Denied";
    private const string AsAdminMessageKey = "Message.KillerTab.History.AsAdmin";
    private const string FailedMessageKey = "Message.KillerTab.History.Failed";
    private const string FailedNoteMessageKey = "Message.KillerTab.History.FailedNote";

    private const string DisabledMessageKey = "Message.KillerTab.History.Disabled";
    private const string DisabledTrayTitleKey = "Title.TaskKiller.Tray.Disabled";
    private const string DisabledTrayMessageKey = "Message.TaskKiller.Tray.Disabled";

    private const int WorkBudget = 4; // kill up to 4 entries per tick; adjust as needed


    private readonly IProcessMonitorService _monitor;
    private readonly IFeatureServices _features;


    private readonly object _gate = new();
    private readonly Dictionary<string, DateTime> _firstSeen = new(StringComparer.OrdinalIgnoreCase);

    private readonly Dictionary<string, int> _killRetries = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _disabledForSession = new(StringComparer.OrdinalIgnoreCase);

    private System.Threading.Timer? _timer;

    // Prevent overlapping timer callbacks
    private int _tickBusy;
    private int _used = 0;

    // Prevent multiple access denied logs per process name
    private bool _accessDenied = false;



    /// <summary>
    /// Create a new instance of the <see cref="TaskKiller"/> service.
    /// </summary>
    /// <param name="monitor">Reference to the process monitor service.</param>
    /// <param name="features">The feature features.</param>
    public TaskKiller(IProcessMonitorService monitor, IFeatureServices features)
    {
        _monitor = monitor;
        _features = features;

        _features.Settings.SettingsApplied += OnSettingsApplied;
        _features.Config.SaveDataUpdated += OnSaveDataUpdated;

    }



    /// <summary>
    /// Starts the timer-driven killer.
    /// </summary>
    public void Start(TimeSpan? initialDelay = null)
    {
        if (_timer != null)
        {
            return;
        }

        var period = GetInterval();
        var due = initialDelay ?? TimeSpan.FromSeconds(1);
        _timer = new System.Threading.Timer(TimerTick!, null, due, period);
    }

    /// <summary>
    /// Stops the killer and clears transient state.
    /// </summary>
    public void Stop()
    {
        lock (_gate)
        {
            _timer?.Dispose();
            _timer = null;
            _firstSeen.Clear();
        }
    }

    private TimeSpan GetInterval()
    {
        // Settings property for process polling cadence.
        var seconds = (double)_features.Settings.Current.ProcessPollingIntervalSeconds;
        if (seconds < 0.1D)
        {
            seconds = 0.1D;
        }

        return TimeSpan.FromSeconds(seconds);
    }


    private TimeSpan GetGraceDelay()
    {
        // Settings property for process termination grace period after first seen.
        var seconds = (double)_features.Settings.Current.ProcessKillerDelaySeconds;
        if (seconds < 0.1D)
        {
            seconds = 0.1D;
        }

        return TimeSpan.FromSeconds(seconds);
    }

    private void TimerTick(object? _)
    {
        // Only one tick at a time
        if (Interlocked.Exchange(ref _tickBusy, 1) == 1)
        {
            return;
        }

        try
        {
            _used = 0; // reset per tick

            if (!_features.Settings.Current.EnableProcessKiller)
            {
                return;
            }

            CheckKillProcesses(out HashSet<string> stillRunning);

            // Cleanup entries that are no longer running at all
            lock (_gate)
            {
                if (_firstSeen.Count > 0)
                {
                    var toRemove = _firstSeen.Keys.Where(k => !stillRunning.Contains(k)).ToList();
                    foreach (var k in toRemove)
                    {
                        _firstSeen.Remove(k);
                    }
                }
            }
        }
        catch
        {
            // ...let it slide
        }
        finally
        {
            Interlocked.Exchange(ref _tickBusy, 0);
        }
    }

    private void CheckKillProcesses(out HashSet<string> stillRunning)
    {

        var grace = GetGraceDelay();

        // consistent snapshot (names -> { Pids, ... })
        var snap = _monitor.Snapshot();

        // Work on a stable copy of the configured killers
        var killers = _features.Config.Current.Killers?.ToArray() ?? [];

        var now = DateTime.UtcNow;

        // Track names still present... prune _firstSeen for those that disappeared
        stillRunning = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var kd in killers)
        {
            if (_used >= WorkBudget)
            {
                break; // finish remaining on next tick
            }

            var entry = kd.Entry;
            if (entry == null || !entry.Enabled)
            {
                continue;
            }

            // Killer entries keyed by process name... name also mirrored in Path to aid uniqueness.
            var name = (entry.Name ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(name))
            {
                continue;
            }

            if (_disabledForSession.Contains(name))
            {
                continue; // disabled for the rest of this session
            }

            if (!snap.TryGetValue(name, out var info))
            {
                // Not running -> ensure any firstSeen timestamp is cleared
                lock (_gate)
                {
                    _firstSeen.Remove(name);
                }

                continue;
            }

            var pids = GetPids(info);
            if (pids.Count == 0)
            {
                lock (_gate)
                {
                    _firstSeen.Remove(name);
                }

                continue;
            }

            stillRunning.Add(name);


            InfoService.TrayData trayData;
            bool killNow = grace <= TimeSpan.Zero;
            if (!killNow)
            {
                DateTime first;
                lock (_gate)
                {
                    if (!_firstSeen.TryGetValue(name, out first))
                    {
                        _firstSeen[name] = now;
                        var graceSeconds = grace.TotalSeconds.ToString("0.#");
                        int attempts = _killRetries.TryGetValue(name, out var x) ? x : 0;
                        if (attempts == 0)
                        {
                            InfoService.LogKillerHistory(_features, DetectedMessageKey, args: [name, graceSeconds]);
                        }

                        continue; // wait one or more ticks
                    }
                }

                if (now - first >= grace)
                {
                    killNow = true;
                }
            }

            if (!killNow)
            {
                continue;
            }


            // Attempt to terminate all known PIDs for this name
            int killed = 0;
            bool loggedError = false; // single error entry at most per 1 process name
            _accessDenied = false; // single denied access entry at most per 1 process name
            foreach (var pid in pids)
            {
                TryEndProcess(name, ref killed, ref loggedError, pid);
            }

            if (killed > 0)
            {
                _used++;
                _killRetries.Remove(name);  // success -> clear retry memory
                var pluralSuffix = killed == 1 ? string.Empty : "s";
                trayData = new InfoService.TrayData
                {
                    TitleKey = KilledTrayTitleKey,
                    MessageKey = KilledTrayMessageKey
                };

                InfoService.TrayNotification(_features, trayData, [name]);
                InfoService.LogKillerHistory(_features, KilledMessageKey, args: [name, killed, pluralSuffix]);
            }
            else if (loggedError)
            {
                // One failure attempt for this *name* this tick
                int attempts = _killRetries.TryGetValue(name, out var x) ? x + 1 : 1;
                _killRetries[name] = attempts;

                int max = _features.Settings.Current.MaxKillerRetries;
                if (max > 0 && attempts >= max)
                {
                    CheckMarkDisabled(name, attempts);
                }
            }

            // reset grace tracking so a quick respawn gets a fresh grace period
            lock (_gate)
            {
                _firstSeen.Remove(name);
            }
        }
    }

    private void CheckMarkDisabled(string name, int attempts)
    {
        // Mark disabled *once* and give UX feedback *once*
        if (_disabledForSession.Add(name))
        {
            var trayData = new InfoService.TrayData
            {
                TitleKey = DisabledTrayTitleKey,
                MessageKey = DisabledTrayMessageKey
            };

            InfoService.TrayNotification(_features, trayData, [name, attempts]);
            InfoService.LogKillerHistory(_features, FailedMessageKey, args: [name]);

            if (_accessDenied)
            {
                InfoService.LogKillerHistory(_features, DeniedMessageKey, args: [name]);

                if (!AppInfo.IsElevated)
                {
                    InfoService.LogKillerHistory(_features, AsAdminMessageKey);
                }
            }

            InfoService.LogKillerHistory(_features, DisabledMessageKey, args: [attempts, name]);
        }
    }

    private void TryEndProcess(string name, ref int killed, ref bool loggedError, int pid)
    {
        try
        {
            using Process proc = Process.GetProcessById(pid);
            if (proc.HasExited)
            {
                return;
            }

            proc.Kill(entireProcessTree: false);
            proc.WaitForExit(10); // wait up to 10ms for exit per pid
            killed++;
        }
        catch (System.ComponentModel.Win32Exception ex) when (ex.NativeErrorCode == 5) // Access denied
        {
            if (loggedError)
            {
                return;
            }

            loggedError = true;
            _accessDenied = true;

            Log.Error(ex, $"Access denied killing process '{name}' (PID {pid}).");
        }
        catch (InvalidOperationException)
        {
            // ...let it slide - already exited between snapshot and kill
        }
        catch (ArgumentException)
        {
            // ...let it slide - PID no longer exists/not running
        }
        catch (Exception ex)
        {
            if (loggedError)
            {
                return;
            }

            loggedError = true;

            InfoService.LogKillerHistory(_features, FailedMessageKey, args: [name]);
            InfoService.LogKillerHistory(_features, FailedNoteMessageKey);

            Log.Error(ex, $"Failed to kill process '{name}' (PID {pid}).");

            // could have somehow exited already -> ignore
        }
    }

    private static List<int> GetPids(object info)
    {
        // The monitor snapshot values expose a Pids collection (used elsewhere);
        // Using reflection to avoid a hard dependency on its concrete type.
        try
        {
            var p = info.GetType().GetProperty("Pids");
            if (p?.GetValue(info) is System.Collections.IEnumerable seq)
            {
                var list = new List<int>();
                foreach (var x in seq)
                {
                    if (x is int i)
                    {
                        list.Add(i);
                    }
                }

                return list;
            }
        }
        catch
        {
            // ...let it slide
        }

        return [];
    }


    private void OnSettingsApplied(object? s, EventArgs e)
    {
        // Update timer period on settings changes
        var period = GetInterval();
        _timer?.Change(TimeSpan.Zero, period);
    }

    private void OnSaveDataUpdated(object? s, EventArgs e)
    {
        var killers = _features.Config.Current.Killers ?? [];
        var current = new HashSet<string>(killers.Select(k => k.Entry?.Name).Where(n => !string.IsNullOrWhiteSpace(n))!,
            StringComparer.OrdinalIgnoreCase);

        foreach (var k in _killRetries.Keys.Where(n => !current.Contains(n)).ToList())
        {
            _killRetries.Remove(k);
        }

        foreach (var n in _disabledForSession.Where(n => !current.Contains(n)).ToList())
        {
            _disabledForSession.Remove(n);
        }
    }


    /// <summary>
    /// Dispose the killer service.
    /// </summary>
    public void Dispose()
    {
        Stop();

        _features.Config.SaveDataUpdated -= OnSaveDataUpdated;
        _features.Settings.SettingsApplied -= OnSettingsApplied;

    }
}
