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

using System.Diagnostics;

namespace TaskMaster.Infrastructure.Processes;

/// <summary>
/// Provides functionality to look up process paths by process name.
/// </summary>
internal static class ProcessLookup
{
    /// <summary>
    /// Gets the first resolved full file path for a process given its name (without extension).<br/>
    /// </summary>
    /// <param name="processName">The name of the process (without .exe)</param>
    /// <returns>A string containing the full file path if resolved; otherwise, <see langword="null"/>.</returns>
    public static string? GetFirstPathForProcessName(string processName)
    {
        if (string.IsNullOrWhiteSpace(processName))
        {
            return null;
        }

        // Get processes by name (bare, without extension)
        Process[] candidates = [];
        try
        {
            candidates = Process.GetProcessesByName(processName);
        }
        catch
        {
            // ignore
        }

        if (candidates.Length == 0)
        {
            return null;
        }

        // Prefer the most recently started instance first
        Array.Sort(candidates, (a, b) =>
        {
            DateTime aStart = SafeStartTime(a);
            DateTime bStart = SafeStartTime(b);
            return bStart.CompareTo(aStart); // desc
        });

        foreach (var p in candidates)
        {
            try
            {
                string? path = ProcessPathResolver.TryResolveProcessPath(p.Id);
                if (!string.IsNullOrWhiteSpace(path))
                {
                    return path;
                }
            }
            catch
            {
                // ignore and try next PID
            }
            finally
            {
                try
                {
                    p.Dispose();
                }
                catch
                {
                    // ...let it slide
                }
            }
        }

        return null;
    }

    private static DateTime SafeStartTime(Process p)
    {
        try
        {
            return p.StartTime;
        }
        catch
        {
            return DateTime.MinValue;
        }
    }
}
