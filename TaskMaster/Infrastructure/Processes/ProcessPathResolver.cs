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
using System.Management;
using System.Runtime.InteropServices;
using System.Text;

namespace TaskMaster.Infrastructure.Processes;

/// <summary>
/// Provides functionality to resolve the full file path of a process given its PID.
/// </summary>
internal static class ProcessPathResolver
{
    /// <summary>
    /// Tries to resolve the full file path of a process given its PID.<br/>
    /// </summary>
    /// <param name="pid">The process ID.</param>
    /// <returns>A string containing the full file path if resolved; otherwise, <see langword="null"/>.</returns>
    public static string? TryResolveProcessPath(int pid)
    {
        try
        {
            using var p = Process.GetProcessById(pid);
            var file = p.MainModule?.FileName;
            if (!string.IsNullOrWhiteSpace(file))
            {
                return file;
            }
        }
        catch
        {
            // ignore
        }

        try
        {
            nint h = OpenProcess(ProcessAccessFlags.PROCESS_QUERY_LIMITED_INFORMATION, false, pid);
            if (h != nint.Zero)
            {
                try
                {
                    var sb = new StringBuilder(260);
                    int size = sb.Capacity;
                    if (QueryFullProcessImageName(h, 0, sb, ref size) && size > 0)
                    {
                        return sb.ToString();
                    }
                }
                finally
                {
                    CloseHandle(h);
                }
            }
        }
        catch
        {
            // ignore
        }

        try
        {
            using var searcher = new ManagementObjectSearcher(
                "SELECT ExecutablePath FROM Win32_Process WHERE ProcessId = " + pid);
            foreach (ManagementObject obj in searcher.Get().Cast<ManagementObject>())
            {
                var path = obj["ExecutablePath"] as string;
                if (!string.IsNullOrWhiteSpace(path))
                {
                    return path;
                }
            }
        }
        catch
        {
            // ignore
        }

        return null;
    }

    [Flags]
    private enum ProcessAccessFlags : uint
    {
        PROCESS_QUERY_LIMITED_INFORMATION = 0x1000
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern nint OpenProcess(ProcessAccessFlags access, bool inheritHandle, int processId);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern bool QueryFullProcessImageName(nint hProcess, int flags, StringBuilder exeName, ref int size);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool CloseHandle(nint hObject);
}
