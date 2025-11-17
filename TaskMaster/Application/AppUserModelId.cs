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

using System.Runtime.InteropServices;

using TaskMaster.Services.Logging;

namespace TaskMaster.Application;

/// <summary>
/// Sets a process-wide AppUserModelID so Windows groups all instances
/// (normal or elevated) under the same taskbar icon.
/// </summary>
internal static class AppUserModelId
{
    /// <summary>
    /// The AppUserModelID value for TaskMaster.<br/>
    /// Format: Company.Product[.SubProduct][.Version]
    /// </summary>
    public const string Value = "SemlerPDX.TaskMaster";


    /// <summary>
    /// Apply the AUMID on process startup.
    /// </summary>
    public static void Apply()
    {
        // HRESULT check optional... swallow failures to avoid blocking startup.
        var hr = SetCurrentProcessExplicitAppUserModelID(Value);
        _ = hr;

        if (hr != 0)
        {
            // Log failure to apply AUMID, ignore and continue startup...
            Log.Warn($"Failed to set AppUserModelID, HRESULT: 0x{0:X8} {hr}");

        }
    }


    [DllImport("shell32.dll", CharSet = CharSet.Unicode, SetLastError = false)]
    private static extern int SetCurrentProcessExplicitAppUserModelID(string appID);
}
