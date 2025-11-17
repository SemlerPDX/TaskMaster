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

namespace TaskMaster.Infrastructure.Processes;

/// <summary>
/// Interface for a service that monitors system processes.
/// </summary>
public interface IProcessMonitorService : IDisposable
{
    /// <summary>
    /// Event fired when the list of monitored processes is updated.
    /// </summary>
    event Action? ProcessesUpdated;

    /// <summary>
    /// Get a snapshot of the currently running processes.
    /// </summary>
    /// <returns>A read-only dictionary mapping process names (without .exe) to ProcessEntry objects.</returns>
    IReadOnlyDictionary<string, ProcessMonitorService.ProcessEntry> Snapshot();

    /// <summary>
    /// Gets the paths of all instances of a given process name from the latest snapshot.
    /// </summary>
    /// <param name="processName">The name of the process to look for.</param>
    /// <returns>The paths of all instances of the specified process name.</returns>
    string[] GetPathsFromSnapshot(string processName);

    /// <summary>
    /// Checks if a process with the specified name is currently running.
    /// </summary>
    /// <param name="processName">The name of the process to check.</param>
    /// <returns>A boolean indicating whether the process is running.</returns>
    bool IsRunning(string processName);

    /// <summary>
    /// Pauses the process monitoring service.
    /// </summary>
    void Pause();

    /// <summary>
    /// Resumes the process monitoring service if it was previously paused.
    /// </summary>
    void Resume();

    /// <summary>
    /// Updates the monitoring interval for checking processes.
    /// </summary>
    /// <param name="seconds">The new interval in seconds.</param>
    void UpdateInterval(double seconds);
}
