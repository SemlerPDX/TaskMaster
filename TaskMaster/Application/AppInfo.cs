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
using System.Reflection;
using System.Security.Principal;

namespace TaskMaster.Application;

/// <summary>
/// Centralized app identity and environment info.
/// </summary>
internal static class AppInfo
{
    private const string AppName = "TaskMaster";
    private const string AppVersionDefault = "1.0.0";

    // ----- Lazy, process-stable facts -----
    private static readonly Lazy<bool> _isElevated = new(() =>
    {
        using var id = WindowsIdentity.GetCurrent();
        var principal = new WindowsPrincipal(id);

        return principal.IsInRole(WindowsBuiltInRole.Administrator);
    });

    private static readonly Lazy<string> _name = new(() =>
    {
        var asm = Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly();
        var product = asm.GetCustomAttribute<AssemblyProductAttribute>()?.Product;
        var title = asm.GetCustomAttribute<AssemblyTitleAttribute>()?.Title;

        return product ?? title ?? asm.GetName().Name ?? AppName;
    });

    private static readonly Lazy<string> _location = new(() =>
    {
        var exePath = Environment.ProcessPath;
        if (!string.IsNullOrEmpty(exePath))
        {
            return exePath!;
        }

        // Fallback to process main module (unusual hosts)
        try
        {
            var main = Process.GetCurrentProcess().MainModule?.FileName;
            if (!string.IsNullOrEmpty(main))
            {
                return main!;
            }
        }
        catch
        {
            // ...let it slide
        }

        // Final fallback
        return Path.Combine(AppContext.BaseDirectory, AppName + ".exe");
    });

    private static readonly Lazy<string> _versionString = new(() =>
    {
        var asm = Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly();
        var info = asm.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
        var ver = asm.GetName().Version?.ToString();

        return info ?? ver ?? AppVersionDefault;
    });


    /// <summary>
    /// <see langword="True"/> when the application is running with elevated privileges (as administrator),
    /// otherwise <see langword="false"/>.
    /// </summary>
    public static bool IsElevated => _isElevated.Value;

    /// <summary>
    /// Gets the application name.
    /// </summary>
    public static string Name => _name.Value;

    /// <summary>
    /// Gets the full path to the application executable.
    /// </summary>
    public static string Location => _location.Value;

    /// <summary>
    /// Gets the application version as a string.
    /// </summary>
    public static string VersionString => _versionString.Value;

    /// <summary>
    /// Gets the application version as a <see cref="System.Version"/> object.
    /// </summary>
    public static Version Version =>
        (Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly()).GetName().Version ?? new Version(1, 0, 0, 0);
}
