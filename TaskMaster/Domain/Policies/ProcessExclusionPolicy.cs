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

using TaskMaster.Infrastructure.Persistence;


namespace TaskMaster.Domain.Policies;

/// <summary>
/// Policy for excluding processes by name.
/// </summary>
public sealed class ProcessExclusionPolicy : IProcessExclusionPolicy, IDisposable
{
    private readonly IProcessExclusionsStore _store;
    private HashSet<string> _names;

    /// <summary>
    /// Fired when the exclusions change (e.g. via Reload).
    /// </summary>
    public event Action? Changed;


    /// <summary>
    /// Gets the names of excluded processes.
    /// </summary>
    public IReadOnlyCollection<string> Names => _names;


    /// <summary>
    /// Initializes a new instance of the <see cref="ProcessExclusionPolicy"/> class.
    /// </summary>
    /// <param name="store">The process exclusions store.</param>
    public ProcessExclusionPolicy(IProcessExclusionsStore store)
    {
        _store = store;
        _names = new HashSet<string>(_store.Load(), StringComparer.OrdinalIgnoreCase);
        _store.Changed += OnStoreChanged;
    }

    /// <summary>
    /// Determines whether the specified bare process name is excluded.
    /// </summary>
    /// <param name="bareName">The bare process name (with or without .exe).</param>
    /// <returns><see langword="True"/> if the process is excluded; otherwise, <see langword="false"/>.</returns>
    public bool IsExcluded(string? bareName)
    {
        if (string.IsNullOrWhiteSpace(bareName))
        {
            return false;
        }

        var s = bareName.EndsWith(".exe", StringComparison.OrdinalIgnoreCase)
                ? bareName[..^4]
                : bareName;

        return _names.Contains(s);
    }

    private void OnStoreChanged()
    {
        _names = new HashSet<string>(_store.Load(), StringComparer.OrdinalIgnoreCase);
        Changed?.Invoke();
    }

    /// <summary>
    /// Disposes the policy and unsubscribes from store changes.
    /// </summary>
    public void Dispose()
    {
        _store.Changed -= OnStoreChanged;
    }
}
