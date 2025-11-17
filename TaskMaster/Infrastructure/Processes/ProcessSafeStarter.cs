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
using System.IO;

using TaskMaster.Application;
using TaskMaster.Services;
using TaskMaster.Services.Logging;

namespace TaskMaster.Infrastructure.Processes;

/// <summary>
/// Provides functionality to safely start processes, ensuring unelevated starts from elevated parents.<br/>
/// Allows secondary launch path via scheduled tasks if indicated in arguments.
/// </summary>
public static class ProcessSafeStarter
{
    private const string DefaultTaskTag = "schtasks";

    /// <summary>
    /// Launch a process with optional arguments and working directory, use of shell execute, and/or verb.<br/>
    /// If this application is elevated, the launched process will be forced to start unelevated to avoid UAC prompts.<br/><br/>
    /// If the <paramref langword="arguments"/> contain the task tag (see <see cref="DefaultTaskTag"/>) and a scheduled task name<br/>to launch (surrounded by double quotes),
    /// the task will be run via <see cref="ScheduledTaskHandler"/>.<br/>
    /// </summary>
    /// <param name="exePath">The full path to the executable.</param>
    /// <param name="arguments">Any command line arguments (optional).</param>
    /// <param name="workingDir">The working directory (optional, defaults to exe's directory).</param>
    /// <param name="useShell">Whether to use the shell to start the process (optional, defaults to false).</param>
    /// <param name="verb">The verb to use when starting the process (optional, e.g. "runas" for admin).</param>
    /// <returns><see langword="True"/> if the process was started successfully, <see langword="false"/> if otherwise.</returns>
    public static bool LaunchProcess(string exePath, string? arguments = null, string? workingDir = null, bool? useShell = false, string? verb = null)
    {
        try
        {
            // New system to launch scheduled tasks via schtasks if indicated in args
            var scheduledTaskName = GetScheduledTaskName(arguments);
            if (scheduledTaskName != null)
            {
                if (ScheduledTaskHandler.TryRunTask(scheduledTaskName))
                {
                    return true;
                }
            }

            if (!IsSafeExecutablePath(exePath))
            {
                // cannot run if unsafe, just send back false - higher can handle additional info or disable if desired
                var process = Path.GetFileName(exePath) ?? "[[MISSING NAME]]";
                Log.Warn($"Unsafe for ProcessSafeStarter to launch process '{process}'");
                return false;
            }

            // Force UNELEVATED start from an elevated parent
            if (AppInfo.IsElevated)
            {
                UnelevatedStart(exePath, arguments, workingDir);
                return true;
            }

            // Normal unelevated start from unelevated parent
            var startInfo = new ProcessStartInfo
            {
                FileName = exePath,
                Arguments = arguments ?? string.Empty,
                WorkingDirectory = workingDir ?? Path.GetDirectoryName(exePath),
                UseShellExecute = useShell ?? false,
                Verb = verb ?? string.Empty
            };

            Process.Start(startInfo);
            return true;
        }
        catch (Exception ex)
        {
            // gracefully handle, just send back false - higher can handle additional info or disable if desired
            Log.Error(ex, "Failed to launch process in ProcessSafeStarter.");
            return false;
        }
    }


    private static string? GetScheduledTaskName(string? arguments)
    {
        if (string.IsNullOrEmpty(arguments))
        {
            return null;
        }

        if (!arguments.Contains(DefaultTaskTag, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var parts = arguments.Split(['"'], 2, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length >= 2)
        {
            var schtasksName = !string.IsNullOrWhiteSpace(parts[1]) ? parts[1].Replace("\"", "").Replace("\\", "") : null;
            return schtasksName;
        }

        return null;
    }

    private static bool IsSafeExecutablePath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return false;
        }

        try
        {
            var full = Path.GetFullPath(path);

            if (!File.Exists(full))
            {
                return false;
            }

            var ext = Path.GetExtension(full);
            if (!string.Equals(ext, ".exe", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            // Disallow UNC paths or network shares
            if (full.StartsWith(@"\\"))
            {
                return false;
            }

            return true;
        }
        catch
        {
            return false;
        }
    }

    private static void UnelevatedStart(string fileName, string? arguments = null, string? workingDirectory = null)
    {
        var shellType = Type.GetTypeFromProgID("Shell.Application");
        if (shellType == null)
        {
            throw new InvalidOperationException("Shell.Application COM object not available.");
        }

        // Delegates to Explorer's medium-IL token.
        dynamic? shell = Activator.CreateInstance(shellType);
        if (shell == null)
        {
            throw new InvalidOperationException("Failed to create Shell.Application.");
        }

        try
        {
            // verb "open" launches with the shell token (unelevated)
            shell.ShellExecute(
                fileName,
                arguments ?? string.Empty,
                workingDirectory ?? string.Empty,
                "open",
                1 // SW_SHOWNORMAL
            );
        }
        finally
        {
            try
            {
                System.Runtime.InteropServices.Marshal.FinalReleaseComObject(shell);
            }
            catch
            {
                // ...let it slide
            }
        }
    }
}
