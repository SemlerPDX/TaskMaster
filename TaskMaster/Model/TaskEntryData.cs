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
/// A simple data structure representing a scheduled task entry.
/// </summary>
public sealed class TaskEntryData
{
    /// <summary>
    /// The name of the task execution.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// The full path of the task execution.
    /// </summary>
    public string Path { get; set; } = string.Empty;

    /// <summary>
    /// The folder containing the task execution.
    /// </summary>
    public string Folder { get; set; } = string.Empty;

    /// <summary>
    /// The current status of the task execution.<br/>
    /// i.e. "Ready, "Running, "Disabled", etc.
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// The next run time of the task execution as a string.
    /// </summary>
    public string NextRunTime { get; set; } = string.Empty;

    /// <summary>
    /// The last run time of the task execution as a string.
    /// </summary>
    public string LastRunTime { get; set; } = string.Empty;

    /// <summary>
    /// The last result of the task execution.<br/>
    /// A numeric code as a string.
    /// </summary>
    public string LastResult { get; set; } = string.Empty;

    /// <summary>
    /// The author of the task.
    /// </summary>
    public string Author { get; set; } = string.Empty;

    /// <summary>
    /// The description of the task.<br/>
    /// CSV "Comment", or if available, XML Description
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// The command to be executed by the task.
    /// </summary>
    public string Command { get; set; } = string.Empty;

    /// <summary>
    /// A string representing the arguments to be passed to the command.
    /// </summary>
    public string Arguments { get; set; } = string.Empty;

    /// <summary>
    /// A string representing the run level requirement.<br/>
    /// "HighestAvailable" or empty string.
    /// </summary>
    public string RunLevel { get; set; } = string.Empty;

    /// <summary>
    /// A string representing the multiple instances policy.<br/>
    /// i.e. "IgnoreNew", "StopExisting", etc.
    /// </summary>
    public string MultipleInstancesPolicy { get; set; } = string.Empty;
}

