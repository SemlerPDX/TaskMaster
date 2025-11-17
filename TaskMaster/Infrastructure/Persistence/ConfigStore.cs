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
/// A service to manage loading and saving application configuration data.
/// </summary>
public sealed class ConfigStore : IConfigStore
{
    private readonly IConfigFileHandler _configHandler;

    /// <summary>
    /// The current application save data.
    /// </summary>
    public SaveData Current { get; private set; }

    /// <summary>
    /// The file path where the configuration data is stored.
    /// </summary>
    public string ConfigPath { get; private set; }

    /// <summary>
    /// Event raised after save data has been applied or updated.
    /// </summary>
    public event EventHandler? SaveDataUpdated;


    /// <summary>
    /// Initializes a new instance of the <see cref="ConfigStore"/> class,
    /// loading application save data from persistent storage.
    /// </summary>
    public ConfigStore() : this(new ConfigFileHandler()) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="ConfigStore"/> class,
    /// loading application save data from persistent storage.
    /// </summary>
    /// <param name="configHandler">The config file handler to use for loading and saving data.</param>
    /// <exception cref="ArgumentNullException">Thrown if the provided config handler is null.</exception>
    public ConfigStore(IConfigFileHandler configHandler)
    {
        _configHandler = configHandler ?? throw new ArgumentNullException(nameof(configHandler));
        ConfigPath = _configHandler.GetConfigPath();
        Current = LoadOrNew();
    }


    /// <summary>
    /// Load the saved data or return a new instance if none exists.
    /// </summary>
    /// <returns>A <see cref="SaveData"/> object representing the current or new application state.</returns>
    public SaveData LoadOrNew()
    {
        return _configHandler.LoadConfig<SaveData>() ?? new SaveData();
    }

    /// <summary>
    /// Save the provided data to persistent storage.
    /// </summary>
    /// <param name="data">The <see cref="SaveData"/> object to be saved.</param>
    public void Save(SaveData data)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(data);

            _configHandler.SaveConfig(data);
            Current = data;

            OnSavedData();
        }
        catch (ArgumentNullException ex)
        {
            Log.Error(ex, "Attempted to save null SaveData object.");
        }
        catch (Exception ex)
        {
            Log.Error(ex, $"An unexpected error occurred while saving SaveData to ConfigPath: '{ConfigPath}'");
        }
    }

    /// <summary>
    /// Delete all config files and reset current data.
    /// </summary>
    public void DeleteAll()
    {
        try
        {
            _configHandler.DeleteAllConfigs();
            Current = new SaveData();

            OnSavedData();
        }
        catch (Exception ex)
        {
            Log.Error(ex, $"An unexpected error occurred while deleting all SaveData in ConfigPath: '{ConfigPath}'");
        }
    }

    private void OnSavedData()
    {
        SaveDataUpdated?.Invoke(this, EventArgs.Empty);
    }
}

