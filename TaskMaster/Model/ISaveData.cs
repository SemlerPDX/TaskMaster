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
/// Interface representing the data structure for saving and loading application state.
/// </summary>
public interface ISaveData
{
    /// <summary>
    /// The schema version of the save file format.
    /// </summary>
    int SchemaVersion { get; }

    /// <summary>
    /// A list of process launcher entries.
    /// </summary>
    List<LauncherData> Launchers { get; set; }

    /// <summary>
    /// A list of process killer entries.
    /// </summary>
    List<KillerData> Killers { get; set; }
}
