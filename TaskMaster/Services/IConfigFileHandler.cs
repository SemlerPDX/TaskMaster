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
/// A service to manage loading and saving application configuration data.
/// Requires that config objects be XML serializable.
/// </summary>
public interface IConfigFileHandler
{
    /// <summary>
    /// Get the full path to the config file in the user's AppData directory.
    /// </summary>
    /// <param name="fileName">The config file name (optional, defaults to "config").</param>
    /// <param name="folderName">The folder name under AppData (optional, defaults to application name).</param>
    /// <returns>The full path to the application config file.</returns>
    string GetConfigPath(string? fileName = null, string? folderName = null);

    /// <summary>
    /// Save the given config object to disk. Must be XML serializable.
    /// </summary>
    /// <typeparam name="T">The type of the config object (must be XML serializable).</typeparam>
    /// <param name="config">The config object to save.</param>
    void SaveConfig<T>(T config);

    /// <summary>
    /// Load the XML serializable config object from disk, returning null if not found or otherwise.
    /// </summary>
    /// <typeparam name="T">The type of the config object (must be XML serializable).</typeparam>
    /// <returns>The loaded config object, or null if not found or otherwise.</returns>
    T? LoadConfig<T>() where T : class;

    /// <summary>
    /// Delete all config files (main and debug) and the config directory if empty.
    /// </summary>
    /// <returns><see langword="True"/> if successful, <see langword="false"/> if otherwise.</returns>
    bool DeleteAllConfigs();
}
