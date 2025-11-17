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

namespace TaskMaster.Infrastructure.Persistence;

/// <summary>
/// A service to manage loading process exclusions from persistent storage.
/// </summary>
public interface IProcessExclusionsStore
{
    /// <summary>
    /// Event raised when the exclusions have changed.
    /// </summary>
    event Action? Changed;

    /// <summary>
    /// Loads the process exclusions from persistent storage.
    /// </summary>
    /// <returns>A read-only collection of excluded process names.</returns>
    IReadOnlyCollection<string> Load();
}
