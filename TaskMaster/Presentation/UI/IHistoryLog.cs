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

namespace TaskMaster.Presentation.UI;

/// <summary>
/// A service to maintain a history log of timestamped messages.
/// </summary>
public interface IHistoryLog
{
    /// <summary>
    /// Gets a read-only collection of history log entries.
    /// </summary>
    ReadOnlyObservableCollection<string> Items { get; }

    /// <summary>
    /// Appends a new entry to the history log with a timestamp.
    /// </summary>
    /// <param name="message">The message to log.</param>
    void Append(string message);

    /// <summary>
    /// Removes the last entry from the history log.
    /// </summary>
    void ClearLastItem();

    /// <summary>
    /// Clears all entries from the history log.
    /// </summary>
    void Clear();

    /// <summary>
    /// Gets or sets the maximum number of entries to retain in the history log.
    /// </summary>
    int MaxEntries { get; set; }
}
