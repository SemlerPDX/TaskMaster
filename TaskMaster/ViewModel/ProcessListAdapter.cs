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

using System.Collections.ObjectModel;
using System.Windows.Data;
using System.Windows.Threading;

using TaskMaster.Domain.Policies;
using TaskMaster.Infrastructure.Processes;
using TaskMaster.Presentation.UI;

namespace TaskMaster.ViewModel;

/// <summary>
/// A view model representing a running process with its name and instance count.
/// </summary>
public sealed class RunningProcessItem : BaseViewModel
{
    private string _name = string.Empty;
    private int _instanceCount;


    /// <summary>
    /// The name of the process.
    /// </summary>
    public string Name
    {
        get { return _name; }
        set
        {
            if (!string.Equals(_name, value, StringComparison.Ordinal))
            {
                _name = value ?? string.Empty;
                OnPropertyChanged(nameof(Name));
            }
        }
    }

    /// <summary>
    /// The number of instances of this process currently running.
    /// </summary>
    public int InstanceCount
    {
        get { return _instanceCount; }
        set
        {
            if (_instanceCount != value)
            {
                _instanceCount = value;
                OnPropertyChanged(nameof(InstanceCount));
            }
        }
    }
}

/// <summary>
/// Adapts the IProcessMonitorService to a ReadOnlyObservableCollection suitable for binding.
/// </summary>
public sealed class ProcessListAdapter : BaseViewModel, IDisposable
{
    private readonly IProcessMonitorService _monitor;
    private readonly IProcessExclusionPolicy _exclusions;
    private readonly IAppVisibility? _visibility;

    private volatile bool _uiIsActive;
    private volatile bool _dirtyWhileHidden;

    private readonly object _lock = new();
    private readonly ObservableCollection<RunningProcessItem> _items = [];
    private readonly ListCollectionView _view;

    private int _updating;


    /// <summary>
    /// The read-only observable collection of running processes, suitable for binding.
    /// </summary>
    public ReadOnlyObservableCollection<RunningProcessItem> Items { get; }



    /// <summary>
    /// Adapts the IProcessMonitorService to a ReadOnlyObservableCollection suitable for binding.
    /// </summary>
    /// <param name="monitor">The process monitor service to get updates from.</param>
    /// <param name="exclusions">A service providing process name exclusions.</param>
    /// <param name="visibility">The application visibility service to optimize updates when hidden (optional).</param>
    /// <exception cref="ArgumentNullException">Thrown if monitor or exclusions is null.</exception>
    public ProcessListAdapter(
        IProcessMonitorService monitor,
        IProcessExclusionPolicy exclusions,
        IAppVisibility? visibility = null)
    {
        _monitor = monitor ?? throw new ArgumentNullException(nameof(monitor));
        _exclusions = exclusions ?? throw new ArgumentNullException(nameof(exclusions));
        _visibility = visibility;

        Items = new ReadOnlyObservableCollection<RunningProcessItem>(_items);

        _view = (ListCollectionView)CollectionViewSource.GetDefaultView(Items);
        _view.CustomSort = Comparer<RunningProcessItem>.Create(
            (a, b) => StringComparer.OrdinalIgnoreCase.Compare(a?.Name, b?.Name));

        if (_visibility != null)
        {
            _visibility.Changed += OnVisibilityChanged;
        }

        _monitor.ProcessesUpdated += OnProcessesUpdated;
        _exclusions.Changed += OnExclusionsChanged;

        // seed once
        RebuildFromSnapshot();
    }



    private void OnVisibilityChanged(object? sender, EventArgs e)
    {
        var nowActive = ComputeIsUiActive();
        var wasActive = _uiIsActive;

        _uiIsActive = nowActive;

        if (!nowActive)
        {
            _dirtyWhileHidden = true;
            return;
        }

        // Just became visible again...
        if (!wasActive && _dirtyWhileHidden)
        {
            _dirtyWhileHidden = false;
            OnProcessesUpdated();
        }
    }

    private void OnExclusionsChanged()
    {
        // a change in the exclusion set should rebuild the visible list
        OnProcessesUpdated();
    }

    private void OnProcessesUpdated()
    {
        if (!_uiIsActive)
        {
            // Skip and rebuild once when visible again
            _dirtyWhileHidden = true;
            return;
        }

        if (Interlocked.Exchange(ref _updating, 1) == 1)
        {
            return;
        }

        try
        {
            RebuildFromSnapshot();
        }
        finally
        {
            Interlocked.Exchange(ref _updating, 0);
        }
    }

    private void RebuildFromSnapshot()
    {
        var snap = _monitor.Snapshot(); // consistent read

        // Desired name -> count, filtered by exclusions
        var desired = snap
            .Where(kv => !_exclusions.IsExcluded(kv.Key))
            .ToDictionary(kv => kv.Key, kv => kv.Value.Pids.Count, StringComparer.OrdinalIgnoreCase);

        void Apply()
        {
            lock (_lock)
            {
                // Remove anything not desired
                for (int i = _items.Count - 1; i >= 0; i--)
                {
                    var it = _items[i];
                    if (!desired.ContainsKey(it.Name))
                    {
                        _items.RemoveAt(i);
                    }
                }

                // Add/update desired items
                foreach (var kv in desired)
                {
                    var name = kv.Key;
                    var count = kv.Value;

                    var existing = _items.FirstOrDefault(x =>
                        string.Equals(x.Name, name, StringComparison.OrdinalIgnoreCase));

                    if (existing == null)
                    {
                        _items.Add(new RunningProcessItem { Name = name, InstanceCount = count });
                    }
                    else
                    {
                        existing.InstanceCount = count; // notifies row
                    }
                }
            }
        }

        var disp = System.Windows.Application.Current?.Dispatcher;
        if (disp != null && !disp.HasShutdownStarted && !disp.HasShutdownFinished)
        {
            if (disp.CheckAccess())
            {
                Apply();
            }
            else
            {
                // Post work without blocking the monitor thread
                disp.BeginInvoke(
                    new Action(Apply),
                    DispatcherPriority.Background);
            }
        }
        else
        {
            // Fallback for design-time / no app dispatcher
            Apply();
        }
    }

    private bool ComputeIsUiActive()
    {
        if (_visibility == null)
        {
            return true;
        }

        return _visibility.IsVisible &&
               !_visibility.IsMinimized &&
               !_visibility.IsInTray;
    }


    /// <summary>
    /// Cleans up event handlers.
    /// </summary>
    public void Dispose()
    {
        _monitor.ProcessesUpdated -= OnProcessesUpdated;
        _exclusions.Changed -= OnExclusionsChanged;

        if (_visibility != null)
        {
            _visibility.Changed -= OnVisibilityChanged;
        }
    }
}
