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

namespace TaskMaster.Domain.Policies;

/// <summary>
/// Policy for excluding processes by name.
/// </summary>
public interface IProcessExclusionPolicy
{
    /// <summary>
    /// Fired when the exclusions change (e.g. via Reload).
    /// </summary>
    event Action? Changed;

    /// <summary>
    /// Gets the names of excluded processes.
    /// </summary>
    IReadOnlyCollection<string> Names { get; }

    /// <summary>
    /// Determines whether the specified bare process name is excluded.
    /// </summary>
    /// <param name="bareName">The bare process name (with or without .exe).</param>
    /// <returns><see langword="True"/> if the process is excluded; otherwise, <see langword="false"/>.</returns>
    bool IsExcluded(string? bareName);
}
