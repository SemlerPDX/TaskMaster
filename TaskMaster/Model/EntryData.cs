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

namespace TaskMaster.Model;

/// <summary>
/// A data structure to hold information about an entry, either for launching or killing.
/// </summary>
public sealed class EntryData
{
    /// <summary>
    /// The display name of the entry.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// The full path to the executable or process name.
    /// </summary>
    public string Path { get; set; } = string.Empty;

    /// <summary>
    /// Any command line arguments (optional).
    /// </summary>
    public string Arguments { get; set; } = string.Empty;

    /// <summary>
    /// A flag indicating if the entry is enabled (default true).
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// A flag indicating if any auxiliary applications should be launched with the main entry (launchers only, default true).
    /// </summary>
    public bool AuxEnabled { get; set; } = true;


    /// <summary>
    /// Default constructor for EntryData.
    /// </summary>
    public EntryData() { }

    /// <summary>
    /// Construct a new EntryData object.
    /// </summary>
    /// <param name="name">The display name of the entry.</param>
    /// <param name="path">The full path to the executable or process name.</param>
    /// <param name="arguments">Any command line arguments (optional).</param>
    /// <param name="enabled">A flag indicating if the entry is enabled (default true).</param>
    /// <param name="auxEnabled">A flag indicating if any auxiliary applications should be launched with the main entry (launchers only, default true).</param>
    public EntryData(
        string name,
        string path,
        string arguments = "",
        bool enabled = true,
        bool auxEnabled = true
    )
    {
        Name = name;
        Path = path;
        Arguments = arguments;
        Enabled = enabled;
        AuxEnabled = auxEnabled;
    }
}