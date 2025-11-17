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
/// A data structure to hold information about a launcher entry, including any auxiliary applications.
/// </summary>
public sealed class LauncherData
{
    /// <summary>
    /// A stable id for unique entry identification.
    /// </summary>
    public Guid Id { get; set; } = Guid.Empty;

    /// <summary>
    /// This item entry data.
    /// </summary>
    public EntryData Entry { get; set; } = new EntryData();

    /// <summary>
    /// Any auxiliary applications to launch with the main entry (optional).
    /// </summary>
    public List<EntryData> AuxApps { get; set; } = [];


    /// <summary>
    /// Default constructor for LauncherData.
    /// </summary>
    public LauncherData() { }

    /// <summary>
    /// Construct a new LauncherData object.
    /// </summary>
    /// <param name="id">The stable id for unique entry identification.</param>
    /// <param name="entry">This item entry data.</param>
    /// <param name="auxApps">Any auxiliary applications to launch with the main entry (optional).</param>
    public LauncherData(
        Guid? id = null,
        EntryData? entry = null,
        List<EntryData>? auxApps = null
    )
    {
        Id = id ?? Guid.NewGuid();
        Entry = entry ?? new EntryData();
        AuxApps = auxApps ?? [];
    }
}
