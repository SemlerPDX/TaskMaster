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

using TaskMaster.Services.Logging;

namespace TaskMaster.Services;

/// <summary>
/// Orchestrates application startup settings between Run entries and Scheduled Tasks.
/// </summary>
public static class StartupSettingsHandler
{
    /// <summary>
    /// Ensures the startup settings are applied for the supplied run entry/task based on the provided parameters.
    /// </summary>
    /// <param name="executablePath">The path to the executable.</param>
    /// <param name="runEntryName">The name of the Run entry / Scheduled Task.</param>
    /// <param name="runAsAdministrator">A bool indicating whether to run as administrator.</param>
    /// <param name="startWithWindows">A bool indicating whether to start with Windows.</param>
    /// <returns><see langword="True"/> if the operation succeeded; otherwise, <see langword="false"/>.</returns>
    public static bool Apply(
        string executablePath,
        string runEntryName,
        bool runAsAdministrator,
        bool startWithWindows)
    {
        try
        {
            var runStore = new RunKeyStore();
            if (runAsAdministrator)
            {
                // Ensure elevated task, then remove Run entry to avoid double-launch.
                ScheduledTaskHandler.CreateTask(
                    runEntryName,
                    executablePath,
                    runOnLogon: startWithWindows,
                    runElevated: true
                );

                runStore.Remove(runEntryName);
            }
            else
            {
                if (!startWithWindows)
                {
                    // Remove both, just in case.
                    runStore.Remove(runEntryName);
                    ScheduledTaskHandler.TryRemoveTask(runEntryName, requireElevation: true);
                    return true;
                }

                // Ensure HKCU Run entry exists, then remove any leftover task.
                runStore.Set(runEntryName, executablePath);
                ScheduledTaskHandler.TryRemoveTask(runEntryName, requireElevation: true);
            }
        }
        catch (Exception ex)
        {
            // Log here - no caller should never be blocked if startup/task creation fails.
            Log.Error(ex, "Failed to apply startup settings.");
            return false;
        }

        return true;
    }
}
