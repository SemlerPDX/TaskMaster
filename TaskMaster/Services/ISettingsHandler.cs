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

 You should have received attribute copy of the GNU General Public License
 along with this program.  If not, see <http://www.gnu.org/licenses/>.
*/

using TaskMaster.Model;

namespace TaskMaster.Services;

/// <summary>
/// A service to manage loading and saving application settings.
/// </summary>
public interface ISettingsHandler
{
    /// <summary>
    /// Load application settings, returning defaults if not found or on error.
    /// </summary>
    /// <returns>A SettingsData object with loaded or default values.</returns>
    SettingsData Load();

    /// <summary>
    /// Delete all application settings from storage.
    /// </summary>
    void Delete();

    /// <summary>
    /// Save the provided applications settings.
    /// </summary>
    /// <param name="settings">The SettingsData object containing settings to save.</param>
    void Save(SettingsData settings);
}
