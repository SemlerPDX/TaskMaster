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

using TaskMaster.Model;

namespace TaskMaster.Infrastructure.Persistence;

/// <summary>
/// Service for managing application settings.
/// </summary>
public interface ISettingsStore
{
    /// <summary>
    /// Event raised after settings have been applied or reset to defaults.
    /// </summary>
    event EventHandler SettingsApplied;

    /// <summary>
    /// The current settings snapshot.
    /// </summary>
    SettingsData Current { get; }

    /// <summary>
    /// Indicates whether the application is in first-time setup mode.
    /// </summary>
    bool IsFirstTimeSetup { get; }

    /// <summary>
    /// Load settings from persistent storage, or return new default settings if none exist.
    /// </summary>
    /// <returns>A SettingsData snapshot from storage or new defaults.</returns>
    SettingsData LoadOrNew();

    /// <summary>
    /// Reload settings from persistent storage.
    /// </summary>
    void Reload();

    /// <summary>
    /// Apply edited settings, save to persistent storage, and update Current.<br/>
    /// Raises SettingsApplied event after applying.
    /// </summary>
    /// <param name="edited">The edited settings to apply.</param>
    void Apply(SettingsData edited);

    /// <summary>
    /// Reset settings to their default values and save to persistent storage.
    /// </summary>
    void ResetToDefaults();

    /// <summary>
    /// Delete all settings from persistent storage.
    /// </summary>
    void DeleteAll();
}
