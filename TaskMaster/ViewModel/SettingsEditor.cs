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

using System.ComponentModel;
using System.Runtime.CompilerServices;

using TaskMaster.Model;
using TaskMaster.Services;

namespace TaskMaster.ViewModel;

/// <summary>
/// Editable, observable wrapper around SettingsData for MVVM forms.
/// </summary>
public sealed class SettingsEditor : ISettingsData, INotifyPropertyChanged
{
    private readonly SettingsData _data;

    /// <summary>
    /// Create a new SettingsEditor initialized from the given seed data.
    /// </summary>
    /// <param name="seed">The seed settings data to copy from.</param>
    public SettingsEditor(SettingsData seed)
    {
        // private copy so persisted settings are not mutated until Save
        _data = new SettingsData();
        PropertyCopier<SettingsData>.Copy(seed, _data);
    }

    /// <summary>
    /// Create a SettingsData instance from the current editor values.
    /// </summary>
    /// <returns>The SettingsData instance.</returns>
    public SettingsData ToSettingsData()
    {
        var outp = new SettingsData();
        PropertyCopier<SettingsData>.Copy(_data, outp);
        return outp;
    }

    /// <summary>
    /// Event fired when a property value changes.
    /// </summary>
    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));


#pragma warning disable IDE2001 // Embedded statements must be on their own line - okay for this property setters editor class


    // --- ISettingsData passthrough with change notifications ---
    /// <summary>
    /// The unique application GUID, per user per install.
    /// </summary>
    public string AppGuid
    {
        get => _data.AppGuid;
        set { if (value != _data.AppGuid) { _data.AppGuid = value; OnPropertyChanged(); } }
    }

    /// <summary>
    /// The UI culture code (e.g., "en-US").
    /// </summary>
    public string UiCulture
    {
        get => _data.UiCulture;
        set { if (value != _data.UiCulture) { _data.UiCulture = value; OnPropertyChanged(); } }
    }

    /// <summary>
    /// The application theme ("Light", "Dark", or "Auto").
    /// </summary>
    public string Theme
    {
        get => _data.Theme;
        set { if (value != _data.Theme) { _data.Theme = value; OnPropertyChanged(); } }
    }

    /// <summary>
    /// The global style corner factor (1, 2, 4, or 8).
    /// </summary>
    public int CornerRadiusFactor
    {
        get => _data.CornerRadiusFactor;
        set { if (value != _data.CornerRadiusFactor) { _data.CornerRadiusFactor = value; OnPropertyChanged(); } }
    }

    /// <summary>
    /// The directory where this application is located.
    /// </summary>
    public string AppDirectory
    {
        get => _data.AppDirectory;
        set { if (value != _data.AppDirectory) { _data.AppDirectory = value; OnPropertyChanged(); } }
    }


    /// <summary>
    /// The delay in seconds before relaunching a process.
    /// </summary>
    public decimal RelaunchDelaySeconds
    {
        get => _data.RelaunchDelaySeconds;
        set { if (value != _data.RelaunchDelaySeconds) { _data.RelaunchDelaySeconds = value; OnPropertyChanged(); } }
    }

    /// <summary>
    /// The maximum number of relaunch retries for a process. (0 = unlimited)
    /// </summary>
    public int MaxRelaunchRetries
    {
        get => _data.MaxRelaunchRetries;
        set { if (value != _data.MaxRelaunchRetries) { _data.MaxRelaunchRetries = value; OnPropertyChanged(); } }
    }

    /// <summary>
    /// The maximum number of end process retries for a process. (0 = unlimited)
    /// </summary>
    public int MaxKillerRetries
    {
        get => _data.MaxKillerRetries;
        set { if (value != _data.MaxKillerRetries) { _data.MaxKillerRetries = value; OnPropertyChanged(); } }
    }

    /// <summary>
    /// The time between detection and action for the process killer to terminate a process.
    /// </summary>
    public decimal ProcessKillerDelaySeconds
    {
        get => _data.ProcessKillerDelaySeconds;
        set { if (value != _data.ProcessKillerDelaySeconds) { _data.ProcessKillerDelaySeconds = value; OnPropertyChanged(); } }
    }

    /// <summary>
    /// The interval in seconds to poll running processes for the process launcher and killer features.
    /// </summary>
    public decimal ProcessPollingIntervalSeconds
    {
        get => _data.ProcessPollingIntervalSeconds;
        set { if (value != _data.ProcessPollingIntervalSeconds) { _data.ProcessPollingIntervalSeconds = value; OnPropertyChanged(); } }

    }

    /// <summary>
    /// The maximum number of history entries to keep.
    /// </summary>
    public int HistoryMaxEntries
    {
        get => _data.HistoryMaxEntries;
        set { if (value != _data.HistoryMaxEntries) { _data.HistoryMaxEntries = value; OnPropertyChanged(); } }
    }

    /// <summary>
    /// The delay in milliseconds before showing tooltips.
    /// </summary>
    public int ToolTipDelay
    {
        get => _data.ToolTipDelay;
        set { if (value != _data.ToolTipDelay) { _data.ToolTipDelay = value; OnPropertyChanged(); } }
    }

    /// <summary>
    /// The duration in milliseconds to show tooltips.
    /// </summary>
    public int ToolTipDuration
    {
        get => _data.ToolTipDuration;
        set { if (value != _data.ToolTipDuration) { _data.ToolTipDuration = value; OnPropertyChanged(); } }
    }

    /// <summary>
    /// The duration in milliseconds to show tray notifications.
    /// </summary>
    public int TrayNotificationsDuration
    {
        get => _data.TrayNotificationsDuration;
        set { if (value != _data.TrayNotificationsDuration) { _data.TrayNotificationsDuration = value; OnPropertyChanged(); } }
    }

    /// <summary>
    /// A flag indicating whether to check for application updates on startup.
    /// </summary>
    public bool CheckForUpdates
    {
        get => _data.CheckForUpdates;
        set { if (value != _data.CheckForUpdates) { _data.CheckForUpdates = value; OnPropertyChanged(); } }
    }

    /// <summary>
    /// A flag indicating whether to start the application with Windows.
    /// </summary>
    public bool StartWithWindows
    {
        get => _data.StartWithWindows;
        set { if (value != _data.StartWithWindows) { _data.StartWithWindows = value; OnPropertyChanged(); } }
    }

    /// <summary>
    /// A flag indicating whether to start the application minimized.
    /// </summary>
    public bool StartMinimized
    {
        get => _data.StartMinimized;
        set { if (value != _data.StartMinimized) { _data.StartMinimized = value; OnPropertyChanged(); } }
    }

    /// <summary>
    /// A flag indicating whether to minimize the application to the system tray.
    /// </summary>
    public bool MinimizeToTray
    {
        get => _data.MinimizeToTray;
        set { if (value != _data.MinimizeToTray) { _data.MinimizeToTray = value; OnPropertyChanged(); } }
    }

    /// <summary>
    /// A flag indicating whether to minimize the application when the window is closed.
    /// </summary>
    public bool MinimizeOnClose
    {
        get => _data.MinimizeOnClose;
        set { if (value != _data.MinimizeOnClose) { _data.MinimizeOnClose = value; OnPropertyChanged(); } }
    }

    /// <summary>
    /// A flag indicating whether to show tray notifications.
    /// </summary>
    public bool ShowTrayNotifications
    {
        get => _data.ShowTrayNotifications;
        set { if (value != _data.ShowTrayNotifications) { _data.ShowTrayNotifications = value; OnPropertyChanged(); } }
    }

    /// <summary>
    /// A flag indicating whether to show mouse-hover tooltips in the UI.
    /// </summary>
    public bool ShowToolTips
    {
        get => _data.ShowToolTips;
        set { if (value != _data.ShowToolTips) { _data.ShowToolTips = value; OnPropertyChanged(); } }
    }

    /// <summary>
    /// A flag indicating whether to remember the window position between sessions.
    /// </summary>
    public bool RememberWindowPosition
    {
        get => _data.RememberWindowPosition;
        set { if (value != _data.RememberWindowPosition) { _data.RememberWindowPosition = value; OnPropertyChanged(); } }
    }

    /// <summary>
    /// A flag indicating whether the application window should always be on top.
    /// </summary>
    public bool AlwaysOnTop
    {
        get => _data.AlwaysOnTop;
        set { if (value != _data.AlwaysOnTop) { _data.AlwaysOnTop = value; OnPropertyChanged(); } }
    }

    /// <summary>
    /// A flag indicating whether to run the application with administrator privileges.
    /// </summary>
    public bool RunAsAdmin
    {
        get => _data.RunAsAdmin;
        set { if (value != _data.RunAsAdmin) { _data.RunAsAdmin = value; OnPropertyChanged(); } }
    }

    /// <summary>
    /// The index of the last active tab in the application UI.
    /// </summary>
    public int LastActiveTab
    {
        get => _data.LastActiveTab;
        set { if (value != _data.LastActiveTab) { _data.LastActiveTab = value; OnPropertyChanged(); } }
    }

    /// <summary>
    /// The width of the application window.
    /// </summary>
    public double WindowWidth
    {
        get => _data.WindowWidth;
        set { if (value != _data.WindowWidth) { _data.WindowWidth = value; OnPropertyChanged(); } }
    }

    /// <summary>
    /// The height of the application window.
    /// </summary>
    public double WindowHeight
    {
        get => _data.WindowHeight;
        set { if (value != _data.WindowHeight) { _data.WindowHeight = value; OnPropertyChanged(); } }
    }

    /// <summary>
    /// The X coordinate of the application window position.
    /// </summary>
    public int WindowPosX
    {
        get => _data.WindowPosX;
        set { if (value != _data.WindowPosX) { _data.WindowPosX = value; OnPropertyChanged(); } }
    }

    /// <summary>
    /// The Y coordinate of the application window position.
    /// </summary>
    public int WindowPosY
    {
        get => _data.WindowPosY;
        set { if (value != _data.WindowPosY) { _data.WindowPosY = value; OnPropertyChanged(); } }
    }

    /// <summary>
    /// The UI scale percentage.
    /// </summary>
    public int UiScalePercent
    {
        get => _data.UiScalePercent;
        set { if (value != _data.UiScalePercent) { _data.UiScalePercent = value; OnPropertyChanged(); } }
    }


    /// <summary>
    /// A flag indicating whether the task launcher feature is enabled.
    /// </summary>
    public bool EnableProcessLauncher
    {
        get => _data.EnableProcessLauncher;
        set { if (value != _data.EnableProcessLauncher) { _data.EnableProcessLauncher = value; OnPropertyChanged(); } }
    }

    /// <summary>
    /// A flag indicating whether the task killer feature is enabled.
    /// </summary>
    public bool EnableProcessKiller
    {
        get => _data.EnableProcessKiller;
        set { if (value != _data.EnableProcessKiller) { _data.EnableProcessKiller = value; OnPropertyChanged(); } }
    }


    /// <summary>
    /// The file path to the configuration file.
    /// </summary>
    public string ConfigFilePath
    {
        get => _data.ConfigFilePath;
        set { if (value != _data.ConfigFilePath) { _data.ConfigFilePath = value; OnPropertyChanged(); } }
    }

    /// <summary>
    /// The minimum level for log file entry output.<br/>
    ///<br/>
    /// Trace = 0<br/>
    /// Debug = 1<br/>
    /// Info = 2<br/>
    /// Warn = 3<br/>
    /// Error = 4<br/>
    /// Fatal = 5
    /// </summary>
    public int LoggingMinimumLevel
    {
        get => _data.LoggingMinimumLevel;
        set { if (value != _data.LoggingMinimumLevel) { _data.LoggingMinimumLevel = value; OnPropertyChanged(); } }

    }

    /// <summary>
    /// A flag indicating whether logging is enabled.
    /// </summary>
    public bool EnableLogging
    {
        get => _data.EnableLogging;
        set { if (value != _data.EnableLogging) { _data.EnableLogging = value; OnPropertyChanged(); } }
    }

    /// <summary>
    /// The file path to the log file.
    /// </summary>
    public string LogFilePath
    {
        get => _data.LogFilePath;
        set { if (value != _data.LogFilePath) { _data.LogFilePath = value; OnPropertyChanged(); } }
    }

    /// <summary>
    /// The maximum size of the log file in megabytes before rotation.
    /// </summary>
    public int MaxLogFileSizeMB
    {
        get => _data.MaxLogFileSizeMB;
        set { if (value != _data.MaxLogFileSizeMB) { _data.MaxLogFileSizeMB = value; OnPropertyChanged(); } }
    }

    /// <summary>
    /// A flag indicating whether to automatically clean up old log files.
    /// </summary>
    public bool AutoCleanLogFile
    {
        get => _data.AutoCleanLogFile;
        set { if (value != _data.AutoCleanLogFile) { _data.AutoCleanLogFile = value; OnPropertyChanged(); } }
    }

    /// <summary>
    /// The maximum number of backup log files to keep when rotating.
    /// </summary>
    public int MaxLogBackupFiles
    {
        get => _data.MaxLogBackupFiles;
        set { if (value != _data.MaxLogBackupFiles) { _data.MaxLogBackupFiles = value; OnPropertyChanged(); } }
    }

#pragma warning restore IDE2001 // Embedded statements must be on their own line - okay for property setters class

}