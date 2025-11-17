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
public interface ISettingsData
{
    /// <summary>
    /// The unique application GUID.
    /// </summary>
    [Setting] string AppGuid { get; set; }

    /// <summary>
    /// The UI culture code (e.g., "en-US").
    /// </summary>
    [Setting] string UiCulture { get; set; }

    /// <summary>
    /// The application theme ("Light", "Dark", or "Auto").
    /// </summary>
    [Setting] string Theme { get; set; }

    /// <summary>
    /// The button corner factor (1, 2, 4, or 8).
    /// </summary>
    [Setting] int CornerRadiusFactor { get; set; }

    /// <summary>
    /// The directory where this application is located.
    /// </summary>
    [Setting] string AppDirectory { get; set; }


    /// <summary>
    /// The delay in seconds before relaunching a process.
    /// </summary>
    [Setting] decimal RelaunchDelaySeconds { get; set; }

    /// <summary>
    /// The maximum number of relaunch retries for a process. (0 = unlimited)
    /// </summary>
    [Setting] int MaxRelaunchRetries { get; set; }

    /// <summary>
    /// The maximum number of end process retries for a process. (0 = unlimited)
    /// </summary>
    [Setting] int MaxKillerRetries { get; set; }

    /// <summary>
    /// The time between detection and action for the process killer to terminate a process.
    /// </summary>
    [Setting] decimal ProcessKillerDelaySeconds { get; set; }

    /// <summary>
    /// The interval in seconds for the process monitor to check for processes to launch or terminate.
    /// </summary>
    [Setting] decimal ProcessPollingIntervalSeconds { get; set; }

    /// <summary>
    /// The maximum number of history entries to keep.
    /// </summary>
    [Setting] int HistoryMaxEntries { get; set; }

    /// <summary>
    /// The delay in milliseconds before showing tooltips.
    /// </summary>
    [Setting] int ToolTipDelay { get; set; }

    /// <summary>
    /// The duration in milliseconds to show tooltips.
    /// </summary>
    [Setting] int ToolTipDuration { get; set; }

    /// <summary>
    /// The duration in milliseconds to show tray notifications.
    /// </summary>
    [Setting] int TrayNotificationsDuration { get; set; }

    /// <summary>
    /// A flag indicating whether to check for application updates on startup.
    /// </summary>
    [Setting] bool CheckForUpdates { get; set; }

    /// <summary>
    /// A flag indicating whether to start the application with Windows.
    /// </summary>
    [Setting] bool StartWithWindows { get; set; }

    /// <summary>
    /// A flag indicating whether to start the application minimized.
    /// </summary>
    [Setting] bool StartMinimized { get; set; }

    /// <summary>
    /// A flag indicating whether to minimize the application to the system tray.
    /// </summary>
    [Setting] bool MinimizeToTray { get; set; }

    /// <summary>
    /// A flag indicating whether to minimize the application when the window is closed.
    /// </summary>
    [Setting] bool MinimizeOnClose { get; set; }

    /// <summary>
    /// A flag indicating whether to show tray notifications.
    /// </summary>
    [Setting] bool ShowTrayNotifications { get; set; }

    /// <summary>
    /// A flag indicating whether to show mouse-hover tooltips in the UI.
    /// </summary>
    [Setting] bool ShowToolTips { get; set; }

    /// <summary>
    /// A flag indicating whether to remember the window position between sessions.
    /// </summary>
    [Setting] bool RememberWindowPosition { get; set; }

    /// <summary>
    /// A flag indicating whether the application window should always be on top.
    /// </summary>
    [Setting] bool AlwaysOnTop { get; set; }

    /// <summary>
    /// A flag indicating whether to run the application with administrator privileges.
    /// </summary>
    [Setting] bool RunAsAdmin { get; set; }


    /// <summary>
    /// The index of the last active tab in the main window.
    /// </summary>
    [Setting] int LastActiveTab { get; set; }

    /// <summary>
    /// The width of the application window.
    /// </summary>
    [Setting] double WindowWidth { get; set; }

    /// <summary>
    /// The height of the application window.
    /// </summary>
    [Setting] double WindowHeight { get; set; }

    /// <summary>
    /// The X coordinate of the application window position.
    /// </summary>
    [Setting] int WindowPosX { get; set; }

    /// <summary>
    /// The Y coordinate of the application window position.
    /// </summary>
    [Setting] int WindowPosY { get; set; }

    /// <summary>
    /// The UI scale percentage.
    /// </summary>
    [Setting] int UiScalePercent { get; set; }


    /// <summary>
    /// A flag indicating whether the task launcher feature is enabled.
    /// </summary>
    [Setting] bool EnableProcessLauncher { get; set; }

    /// <summary>
    /// A flag indicating whether the task killer feature is enabled.
    /// </summary>
    [Setting] bool EnableProcessKiller { get; set; }


    /// <summary>
    /// The file path to the configuration file.
    /// </summary>
    [Setting] string ConfigFilePath { get; set; }

    /// <summary>
    /// A flag indicating whether logging is enabled.
    /// </summary>
    [Setting] bool EnableLogging { get; set; }

    /// <summary>
    /// The file path to the log file.
    /// </summary>
    [Setting] string LogFilePath { get; set; }

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
    [Setting] int LoggingMinimumLevel { get; set; }

    /// <summary>
    /// The maximum size of the log file in megabytes before rotation.
    /// </summary>
    [Setting] int MaxLogFileSizeMB { get; set; }

    /// <summary>
    /// A flag indicating whether to automatically clean up old log files.
    /// </summary>
    [Setting] bool AutoCleanLogFile { get; set; }

    /// <summary>
    /// The maximum number of backup log files to keep when rotating.
    /// </summary>
    [Setting] int MaxLogBackupFiles { get; set; }
}
