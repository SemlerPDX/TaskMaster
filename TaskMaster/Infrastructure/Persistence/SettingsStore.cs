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
using TaskMaster.Services;
using TaskMaster.Services.Logging;

namespace TaskMaster.Infrastructure.Persistence;

/// <summary>
/// Service for managing application settings.
/// </summary>
public sealed class SettingsStore : ISettingsStore
{
    private readonly ISettingsHandler _settingsHandler;

    private readonly object _sync = new();


    /// <summary>
    /// Event raised after settings have been applied or reset to defaults.
    /// </summary>
    public event EventHandler? SettingsApplied;


    /// <summary>
    /// The current application settings.
    /// </summary>
    public SettingsData Current { get; private set; }

    /// <summary>
    /// Indicates whether the application is in first-time setup mode.
    /// </summary>
    public bool IsFirstTimeSetup { get; private set; } = false;



    /// <summary>
    /// Initializes a new instance of the <see cref="ConfigStore"/> class,
    /// loading application save data from persistent storage.
    /// </summary>
    public SettingsStore() : this(new SettingsHandler()) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="SettingsStore"/> class,
    /// loading <see cref="SettingsData"/> from persistent storage.
    /// </summary>
    /// <param name="settingsHandler">The settings handler to use for loading and saving data.</param>
    /// <exception cref="ArgumentNullException">Thrown if the provided settings handler is null.</exception>
    public SettingsStore(ISettingsHandler settingsHandler)
    {
        _settingsHandler = settingsHandler ?? throw new ArgumentNullException(nameof(settingsHandler));
        Current = LoadOrNew();
    }


    /// <summary>
    /// Load settings from persistent storage, or return new default settings if none exist.
    /// </summary>
    /// <returns>The current <see cref="SettingsData"/> snapshot from storage or new defaults.</returns>
    public SettingsData LoadOrNew()
    {
        var settings = _settingsHandler.Load() ?? new SettingsData();

        IsFirstTimeSetup = string.IsNullOrWhiteSpace(settings.AppGuid);

        return settings;
    }

    /// <summary>
    /// Reload settings from persistent storage.
    /// </summary>
    public void Reload()
    {
        lock (_sync)
        {
            Current = _settingsHandler.Load();
        }

        OnSettingsApplied();
    }

    /// <summary>
    /// Apply edited settings, save to persistent storage, and update Current.
    /// </summary>
    /// <param name="edited">The edited settings to apply.</param>
    public void Apply(SettingsData edited)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(edited);

            lock (_sync)
            {
                PropertyCopier<SettingsData>.Copy(edited, Current);

                _settingsHandler.Save(Current);
            }

            OnSettingsApplied();
        }
        catch (ArgumentNullException ex)
        {
            Log.Error(ex, "Attempted to save null SettingsData object.");
        }
        catch (Exception ex)
        {
            Log.Error(ex, $"An unexpected error occurred while saving SettingsData.");
        }
    }

    /// <summary>
    /// Reset settings to their default values and save to persistent storage.
    /// </summary>
    public void ResetToDefaults()
    {
        lock (_sync)
        {
            Current = new SettingsData(); // default ctor defines defaults
            _settingsHandler.Save(Current);
        }

        OnSettingsApplied();
    }

    /// <summary>
    /// Delete all application settings from storage and reset to defaults.
    /// </summary>
    public void DeleteAll()
    {
        try
        {
            lock (_sync)
            {
                _settingsHandler.Delete();
                Current = new SettingsData();
            }

            OnSettingsApplied();
        }
        catch (Exception ex)
        {
            Log.Error(ex, $"An unexpected error occurred while deleting all SettingsData.");
        }
    }

    private void OnSettingsApplied()
    {
        SettingsApplied?.Invoke(this, EventArgs.Empty);
    }
}
