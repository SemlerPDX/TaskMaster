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

using TaskMaster.Application.Composition;
using TaskMaster.Infrastructure.Processes;
using TaskMaster.Presentation.UI;
using TaskMaster.Services;

namespace TaskMaster.Application.Features;

/// <summary>
/// Background service that monitors and (re)launches specified applications,
/// honoring a grace delay after an app is first detected as missing.<br/>
/// Also manages retry attempts and max retry limits per settings.
/// </summary>
public sealed class TaskLauncher : ITaskLauncher, IDisposable
{
    private const string MissingMessageKey = "Message.LauncherTab.History.Missing";

    private const string LaunchedMessageKey = "Message.LauncherTab.History.Launched";
    private const string LaunchedTrayTitleKey = "Title.TaskLauncher.Tray.Launched";
    private const string LaunchedTrayMessageKey = "Message.TaskLauncher.Tray.Launched";

    private const string FailedMessageKey = "Message.LauncherTab.History.Failed";
    private const string FailedTrayTitleKey = "Title.TaskLauncher.Tray.Failed";
    private const string FailedTrayMessageKey = "Message.TaskLauncher.Tray.Failed";

    private const string DisabledMessageKey = "Message.LauncherTab.History.Disabled";
    private const string DisabledTrayTitleKey = "Title.TaskLauncher.Tray.Disabled";
    private const string DisabledTrayMessageKey = "Message.TaskLauncher.Tray.Disabled";

    private readonly IProcessMonitorService _monitor;
    private readonly IFeatureServices _features;


    private CancellationTokenSource? _cts;

    private readonly Dictionary<string, int> _retries = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, DateTime> _firstMissing = new(StringComparer.OrdinalIgnoreCase);


    /// <summary>
    /// Create a new launcher service.
    /// </summary>
    /// <param name="monitor">The process monitor service.</param>
    /// <param name="features">The feature services.</param>
    public TaskLauncher(IProcessMonitorService monitor, IFeatureServices features)
    {
        _monitor = monitor;
        _features = features;

        _features.Config.SaveDataUpdated += OnSaveDataUpdated;
    }


    /// <summary>
    /// Start the background launcher service.
    /// </summary>
    public void Start(TimeSpan? initialDelay = null)
    {
        if (_cts != null)
        {
            return;
        }

        var startupDelay = initialDelay ?? TimeSpan.FromSeconds(5);
        _cts = new CancellationTokenSource();
        _ = Task.Run(() => LoopAsync(startupDelay, _cts.Token));
    }

    /// <summary>
    /// Stop the background launcher service.
    /// </summary>
    public void Stop()
    {
        _cts?.Cancel();
        _cts = null;
    }


    private async Task LoopAsync(TimeSpan startupDelay, CancellationToken token)
    {
        try
        {
            // Initial feature startup delay before first check
            await Task.Delay(startupDelay, token);

            var pollingInterval = GetInterval();
            while (!token.IsCancellationRequested)
            {
                await Task.Delay(pollingInterval, token);

                await CheckLaunchProcesses(token);
            }
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested)
        {
            // graceful exit
        }
        catch
        {
            // ...let it slide
        }
    }

    private async Task CheckLaunchProcesses(CancellationToken token)
    {
        var launchPace = GetInterval();

        // Snapshot of current configured launchers
        foreach (var launcherData in _features.Config.Current.Launchers.ToArray())
        {
            if (token.IsCancellationRequested)
            {
                break;
            }

            // Primary + Aux launch (with special "aux-only when main disabled" mode)
            var entry = launcherData.Entry;
            bool mainEnabled = entry.Enabled;
            bool auxBlockEnabled = entry.AuxEnabled;
            bool hasAuxList = launcherData.AuxApps is { Count: > 0 };

            // === SPECIAL MODE ================================================
            // If the target app is NOT enabled, but its Aux block IS enabled,
            // switch to "detection only": do NOT relaunch target; instead,
            // if target is currently running, ensure aux apps are up.
            if (!mainEnabled && auxBlockEnabled && hasAuxList)
            {
                if (_monitor.IsRunning(entry.Name))
                {
                    await TryLaunchAuxApps(launchPace, launcherData, token,
                        requireMainRunning: () => _monitor.IsRunning(entry.Name));
                }

                // Skip normal path entirely for this launcher row
                continue;
            }
            // ================================================================

            // Normal mode: launch main if enabled
            if (mainEnabled)
            {
                TryLaunchWithGrace(
                    name: entry.Name,
                    path: entry.Path,
                    arguments: entry.Arguments);
            }

            if (!auxBlockEnabled || !hasAuxList)
            {
                continue;
            }

            await TryLaunchAuxApps(launchPace, launcherData, token,
                requireMainRunning: () => _monitor.IsRunning(entry.Name));
        }
    }

    private async Task TryLaunchAuxApps(
        TimeSpan launchPace,
        Model.LauncherData launcherData,
        CancellationToken token,
        Func<bool>? requireMainRunning = null)
    {
        foreach (var aux in launcherData.AuxApps!.ToArray())
        {
            if (token.IsCancellationRequested)
            {
                break;
            }

            if (requireMainRunning != null && !requireMainRunning())
            {
                // target died mid-loop; bail out cleanly
                break;
            }

            if (!aux.Enabled)
            {
                continue;
            }

            await Task.Delay(launchPace, token);

            TryLaunchWithGrace(
                name: aux.Name,
                path: aux.Path,
                arguments: aux.Arguments);
        }
    }

    private static string RetryKey(string path, string arguments)
    {
        return string.Concat(path?.Trim() ?? string.Empty, "|", arguments?.Trim() ?? string.Empty);
    }

    private void TryLaunchWithGrace(string name, string path, string arguments)
    {
        // If running, clear first-missing state and retry counter
        if (_monitor.IsRunning(name))
        {
            _firstMissing.Remove(name);

            var key = RetryKey(path, arguments);
            _retries.Remove(key);
            return;
        }

        // Not running — manage "first seen missing" timestamp
        var now = DateTime.UtcNow;
        var grace = GetGraceDelay();

        if (!_firstMissing.TryGetValue(name, out var first))
        {
            _firstMissing[name] = now;

            if (grace > TimeSpan.Zero)
            {
                InfoService.LogLauncherHistory(_features, MissingMessageKey, args: [name, grace.TotalSeconds.ToString("0.#")]);
            }

            return; // wait for grace to elapse
        }

        // Still missing but grace not yet elapsed
        if (now - first < grace)
        {
            return;
        }

        // Past grace — try launch (with retry ceiling)
        var rk = RetryKey(path, arguments);
        if (_retries.TryGetValue(rk, out var attempts) &&
            _features.Settings.Current.MaxRelaunchRetries > 0 &&
            attempts >= _features.Settings.Current.MaxRelaunchRetries)
        {
            return;
        }

        InfoService.TrayData trayData;
        bool launched = ProcessSafeStarter.LaunchProcess(path, arguments);

        if (launched)
        {
            // Process has been launched - send messages to history (and tray, when enabled)
            trayData = new InfoService.TrayData
            {
                TitleKey = LaunchedTrayTitleKey,
                MessageKey = LaunchedTrayMessageKey
            };

            InfoService.TrayNotification(_features, trayData, [name]);
            InfoService.LogLauncherHistory(_features, LaunchedMessageKey, args: [name]);

            _firstMissing.Remove(name);
            _retries.Remove(rk);
            return;
        }

        attempts = _retries.TryGetValue(rk, out var x) ? x + 1 : 1;
        _retries[rk] = attempts;

        // Process failed to launch, will keep trying until MAX attempts reached - send messages to history (and tray, when enabled)
        trayData = new InfoService.TrayData
        {
            TitleKey = FailedTrayTitleKey,
            MessageKey = FailedTrayMessageKey,
            MessageType = TrayIconKind.Warning
        };

        InfoService.TrayNotification(_features, trayData, [name, attempts]);
        InfoService.LogLauncherHistory(_features, FailedMessageKey, args: [name, attempts]);


        if (_features.Settings.Current.MaxRelaunchRetries > 0 && attempts >= _features.Settings.Current.MaxRelaunchRetries)
        {
            // Process is now disabled for this session, will stop trying to launch - send messages to history (and tray, when enabled)
            trayData = new InfoService.TrayData
            {
                TitleKey = DisabledTrayTitleKey,
                MessageKey = DisabledTrayMessageKey,
                MessageType = TrayIconKind.Error
            };

            InfoService.TrayNotification(_features, trayData, [name, attempts]);
            InfoService.LogLauncherHistory(_features, DisabledMessageKey, args: [attempts, name]);
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
        // Settings property for process launcher grace period after first seen.
        var seconds = (double)_features.Settings.Current.RelaunchDelaySeconds;
        if (seconds < 0.1D)
        {
            seconds = 0.1D;
        }

        return TimeSpan.FromSeconds(seconds);
    }


    private void OnSaveDataUpdated(object? sender, EventArgs e)
    {
        // Clear retry and first-missing state for any launchers no longer present
        var currentNames = new HashSet<string>(
            _features.Config.Current.Launchers.Select(l => l.Entry.Name)
            .Concat(_features.Config.Current.Launchers.Where(l => l.Entry.AuxEnabled && l.AuxApps is { Count: > 0 })
                                                 .SelectMany(l => l.AuxApps!.Select(a => a.Name))),
            StringComparer.OrdinalIgnoreCase);

        var toRemoveRetries = _retries.Keys
            .Where(k => !currentNames.Contains(k.Split('|')[0]))
            .ToList();

        foreach (var key in toRemoveRetries)
        {
            _retries.Remove(key);
        }

        var toRemoveFirstMissing = _firstMissing.Keys
            .Where(n => !currentNames.Contains(n))
            .ToList();

        foreach (var name in toRemoveFirstMissing)
        {
            _firstMissing.Remove(name);
        }
    }


    /// <summary>
    /// Dispose the launcher service.
    /// </summary>
    public void Dispose()
    {
        Stop();

        _features.Config.SaveDataUpdated -= OnSaveDataUpdated;
    }
}
