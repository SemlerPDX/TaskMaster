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

namespace TaskMaster.Model;

/// <summary>
/// A simple data structure to hold application settings.
/// </summary>
public sealed class SettingsData : ISettingsData
{
    /// <summary>
    /// The unique application GUID.
    /// </summary>
    [Setting] public string AppGuid { get; set; } = "";

    /// <summary>
    /// The UI culture code (e.g., "en-US").
    /// </summary>
    [Setting] public string UiCulture { get; set; } = "en-US";

    /// <summary>
    /// The application theme ("Light", "Dark", or "Auto").
    /// </summary>
    [Setting] public string Theme { get; set; } = "Auto";

    /// <summary>
    /// The button corner factor (1, 2, 4, or 8).
    /// </summary>
    [Setting] public int CornerRadiusFactor { get; set; } = 2;


    /// <summary>
    /// The directory where this application is located.
    /// </summary>
    [Setting] public string AppDirectory { get; set; } = "";


    /// <summary>
    /// The delay in seconds before relaunching a process.
    /// </summary>
    [Setting] public decimal RelaunchDelaySeconds { get; set; } = 5.0M;

    /// <summary>
    /// The maximum number of relaunch retries for a process. (0 = unlimited)
    /// </summary>
    [Setting] public int MaxRelaunchRetries { get; set; } = 3;

    /// <summary>
    /// The maximum number of end process retries for a process. (0 = unlimited)
    /// </summary>
    [Setting] public int MaxKillerRetries { get; set; } = 3;

    /// <summary>
    /// The time between detection and action for the process killer to terminate a process.
    /// </summary>
    [Setting] public decimal ProcessKillerDelaySeconds { get; set; } = 1.0M;

    /// <summary>
    /// The interval in seconds for the process monitor to check for processes to launch or terminate.
    /// </summary>
    [Setting] public decimal ProcessPollingIntervalSeconds { get; set; } = 0.5M;

    /// <summary>
    /// The maximum number of history entries to keep.
    /// </summary>
    [Setting] public int HistoryMaxEntries { get; set; } = 1000;

    /// <summary>
    /// The delay in milliseconds before showing tooltips.
    /// </summary>
    [Setting] public int ToolTipDelay { get; set; } = 1750;

    /// <summary>
    /// The duration in milliseconds to show tooltips.
    /// </summary>
    [Setting] public int ToolTipDuration { get; set; } = 4000;

    /// <summary>
    /// The duration in milliseconds to show tray notifications.
    /// </summary>
    [Setting] public int TrayNotificationsDuration { get; set; } = 5000;

    /// <summary>
    /// A flag indicating whether to check for application updates on startup.
    /// </summary>
    [Setting] public bool CheckForUpdates { get; set; } = true;

    /// <summary>
    /// A flag indicating whether to start the application with Windows.
    /// </summary>
    [Setting] public bool StartWithWindows { get; set; } = false;

    /// <summary>
    /// A flag indicating whether to start the application minimized.
    /// </summary>
    [Setting] public bool StartMinimized { get; set; } = false;

    /// <summary>
    /// A flag indicating whether to minimize the application to the system tray.
    /// </summary>
    [Setting] public bool MinimizeToTray { get; set; } = false;

    /// <summary>
    /// A flag indicating whether to minimize the application when the window is closed.
    /// </summary>
    [Setting] public bool MinimizeOnClose { get; set; } = false;

    /// <summary>
    /// A flag indicating whether to show tray notifications.
    /// </summary>
    [Setting] public bool ShowTrayNotifications { get; set; } = false;

    /// <summary>
    /// A flag indicating whether to show mouse-hover tooltips in the UI.
    /// </summary>
    [Setting] public bool ShowToolTips { get; set; } = false;

    /// <summary>
    /// A flag indicating whether to remember the window position between sessions.
    /// </summary>
    [Setting] public bool RememberWindowPosition { get; set; } = true;

    /// <summary>
    /// A flag indicating whether the application window should always be on top.
    /// </summary>
    [Setting] public bool AlwaysOnTop { get; set; } = false;

    /// <summary>
    /// A flag indicating whether to run the application with administrator privileges.
    /// </summary>
    [Setting] public bool RunAsAdmin { get; set; } = false;


    /// <summary>
    /// The index of the last active tab in the main window.
    /// </summary>
    [Setting] public int LastActiveTab { get; set; } = 2;

    /// <summary>
    /// The width of the application window.
    /// </summary>
    [Setting] public double WindowWidth { get; set; } = 860D;

    /// <summary>
    /// The height of the application window.
    /// </summary>
    [Setting] public double WindowHeight { get; set; } = 560D;

    /// <summary>
    /// The X coordinate of the application window position.
    /// </summary>
    [Setting] public int WindowPosX { get; set; } = 720;

    /// <summary>
    /// The Y coordinate of the application window position.
    /// </summary>
    [Setting] public int WindowPosY { get; set; } = 240;

    /// <summary>
    /// The UI scale percentage.
    /// </summary>
    [Setting] public int UiScalePercent { get; set; } = 100;


    /// <summary>
    /// A flag indicating whether the task launcher feature is enabled.
    /// </summary>
    [Setting] public bool EnableProcessLauncher { get; set; } = true;

    /// <summary>
    /// A flag indicating whether the task killer feature is enabled.
    /// </summary>
    [Setting] public bool EnableProcessKiller { get; set; } = true;


    /// <summary>
    /// The file path to the configuration file.
    /// </summary>
    [Setting] public string ConfigFilePath { get; set; } = "config.dat";

    /// <summary>
    /// A flag indicating whether logging is enabled.
    /// </summary>
    [Setting] public bool EnableLogging { get; set; } = true;

    /// <summary>
    /// The maximum size of the log file in megabytes before rotation.
    /// </summary>
    [Setting] public int MaxLogFileSizeMB { get; set; } = 4;

    /// <summary>
    /// The file path to the log file.
    /// </summary>
    [Setting] public string LogFilePath { get; set; } = "TaskMaster.log";

    /// <summary>
    /// The minimum level for log file entry output.<br/>
    ///<br/>
    /// Trace = 0<br/>
    /// Debug = 1<br/>
    /// Info = 2<br/>
    /// Warn = 3 (default)<br/>
    /// Error = 4<br/>
    /// Fatal = 5
    /// </summary>
    [Setting] public int LoggingMinimumLevel { get; set; } = 3;

    /// <summary>
    /// A flag indicating whether to automatically clean up old log files.
    /// </summary>
    [Setting] public bool AutoCleanLogFile { get; set; } = true;

    /// <summary>
    /// The maximum number of backup log files to keep when rotating.
    /// </summary>
    [Setting] public int MaxLogBackupFiles { get; set; } = 3;
}
