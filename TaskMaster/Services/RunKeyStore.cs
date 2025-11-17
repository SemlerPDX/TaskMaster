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

using Microsoft.Win32;

namespace TaskMaster.Services;

/// <summary>
/// A single entry in the HKCU Run key.
/// </summary>
public sealed class RunEntry
{
    /// <summary>
    /// The name of the Run entry.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// The command line to execute.
    /// </summary>
    public string Command { get; set; } = string.Empty;
}


/// <summary>
/// Per-user startup manager for HKCU\Software\Microsoft\Windows\CurrentVersion\Run.<br/>
/// No elevation required.
/// </summary>
public sealed class RunKeyStore : IRunKeyStore
{
    private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";

    /// <summary>
    /// Lists all Run entries in HKCU under <see cref="RunKeyPath"/>.
    /// </summary>
    /// <returns>An enumerable of <see cref="RunEntry"/> representing Windows Run startup tasks found.</returns>
    public IEnumerable<RunEntry> List()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: false);
        if (key == null)
        {
            yield break;
        }

        foreach (var name in key.GetValueNames())
        {
            var cmd = key.GetValue(name) as string ?? string.Empty;
            yield return new RunEntry { Name = name, Command = cmd };
        }
    }

    /// <summary>
    /// Try to get a Run entry by name.
    /// </summary>
    /// <param name="name">The name of the Run entry.</param>
    /// <param name="entry">The output RunEntry if found.</param>
    /// <returns><see langword="True"/> if found; otherwise, <see langword="false"/>.</returns>
    public bool TryGet(string name, out RunEntry entry)
    {
        entry = new RunEntry();

        using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: false);
        if (key == null)
        {
            return false;
        }

        var cmd = key.GetValue(name) as string;
        if (string.IsNullOrWhiteSpace(cmd))
        {
            return false;
        }

        entry = new RunEntry { Name = name, Command = cmd };
        return true;
    }

    /// <summary>
    /// Sets or updates a Run entry.
    /// </summary>
    /// <param name="name">The name of the Run entry.</param>
    /// <param name="executablePath">The path to the executable.</param>
    /// <param name="arguments">Optional arguments for the executable.</param>
    /// <exception cref="ArgumentException">Thrown if the name is null or whitespace.</exception>
    /// <exception cref="InvalidOperationException">Thrown if unable to create the Run key.</exception>
    public void Set(string name, string executablePath, string? arguments = null)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Entry name is required.", nameof(name));
        }

        var commandLine = BuildCommandLine(executablePath, arguments);

        using var key = Registry.CurrentUser.CreateSubKey(RunKeyPath, writable: true);
        if (key == null)
        {
            throw new InvalidOperationException("Unable to create HKCU Run key.");
        }

        key.SetValue(name, commandLine, RegistryValueKind.String);
    }

    /// <summary>
    /// Removes a Run entry by name.
    /// </summary>
    /// <param name="name">The name of the Run entry to remove.</param>
    public void Remove(string name)
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: true);
        if (key == null)
        {
            return;
        }

        key.DeleteValue(name, throwOnMissingValue: false);
    }

    /// <summary>
    /// Ensures a Run entry is present or absent based on the enable flag.
    /// </summary>
    /// <param name="name">The name of the Run entry.</param>
    /// <param name="enable">A flag indicating whether to enable (add/update) or disable (remove) the entry.</param>
    /// <param name="executablePath">The path to the executable.</param>
    /// <param name="arguments">Optional arguments for the executable.</param>
    public void Ensure(string name, bool enable, string executablePath, string? arguments = null)
    {
        if (enable)
        {
            Set(name, executablePath, arguments);
            return;
        }

        Remove(name);
    }

    private static string BuildCommandLine(string executablePath, string? arguments)
    {
        if (string.IsNullOrWhiteSpace(executablePath))
        {
            // Safe fallback to current process
            executablePath = Environment.ProcessPath
                ?? Process.GetCurrentProcess().MainModule?.FileName
                ?? string.Empty;
        }

        var exe = executablePath.Trim();
        if (string.IsNullOrWhiteSpace(exe))
        {
            throw new InvalidOperationException("Cannot determine executable path.");
        }

        if (!exe.StartsWith("\"", StringComparison.Ordinal))
        {
            exe = $"\"{exe}\"";
        }

        if (!string.IsNullOrWhiteSpace(arguments))
        {
            return exe + " " + arguments.Trim();
        }

        return exe;
    }
}
