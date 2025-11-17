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

namespace TaskMaster.Services;

/// <summary>
/// Interface for Per-user startup manager for HKCU\Software\Microsoft\Windows\CurrentVersion\Run.<br/>
/// No elevation required.
/// </summary>
public interface IRunKeyStore
{
    /// <summary>
    /// Lists all Run entries in HKCU under <see cref="RunKeyStore.RunKeyPath"/>.
    /// </summary>
    /// <returns>An enumerable of <see cref="RunEntry"/> representing Windows Run startup tasks found.</returns>
    IEnumerable<RunEntry> List();

    /// <summary>
    /// Try to get a Run entry by name.
    /// </summary>
    /// <param name="name">The name of the Run entry.</param>
    /// <param name="entry">The output RunEntry if found.</param>
    /// <returns><see langword="True"/> if found; otherwise, <see langword="false"/>.</returns>
    bool TryGet(string name, out RunEntry entry);

    /// <summary>
    /// Sets or updates a Run entry.
    /// </summary>
    /// <param name="name">The name of the Run entry.</param>
    /// <param name="executablePath">The path to the executable.</param>
    /// <param name="arguments">Optional arguments for the executable.</param>
    /// <exception cref="ArgumentException">Thrown if the name is null or whitespace.</exception>
    /// <exception cref="InvalidOperationException">Thrown if unable to create the Run key.</exception>
    void Set(string name, string executablePath, string? arguments = null);

    /// <summary>
    /// Removes a Run entry by name.
    /// </summary>
    /// <param name="name">The name of the Run entry to remove.</param>
    void Remove(string name);

    /// <summary>
    /// Ensures a Run entry is present or absent based on the enable flag.
    /// </summary>
    /// <param name="name">The name of the Run entry.</param>
    /// <param name="enable">A flag indicating whether to enable (add/update) or disable (remove) the entry.</param>
    /// <param name="executablePath">The path to the executable.</param>
    /// <param name="arguments">Optional arguments for the executable.</param>
    void Ensure(string name, bool enable, string executablePath, string? arguments = null);
}
