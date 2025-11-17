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

namespace TaskMaster.Presentation.UI;

/// <summary>
/// Kinds of tray icon notifications.
/// </summary>
public enum TrayIconKind
{
    /// <summary>
    /// No icon (default).
    /// </summary>
    None,

    /// <summary>
    /// Informational icon.
    /// </summary>
    Info,

    /// <summary>
    /// Warning icon.
    /// </summary>
    Warning,

    /// <summary>
    /// Error icon.
    /// </summary>
    Error
}

/// <summary>
/// Centralized, settings-aware tray notification service.
/// View owns the NotifyIcon; service is injected into VMs.
/// </summary>
public interface ITrayNotifications
{
    /// <summary>
    /// Attaches the view-owned NotifyIcon (call from MainWindow).
    /// </summary>
    /// <param name="notifyIcon">The NotifyIcon to attach.</param>
    /// <exception cref="ArgumentNullException">Thrown if notifyIcon is null.</exception>
    void Attach(NotifyIcon notifyIcon);

    /// <summary>
    /// Detaches the previously-attached NotifyIcon.
    /// </summary>
    void Detach();

    /// <summary>Current effective enablement (reflects settings).</summary>
    bool IsEnabled { get; }

    /// <summary>
    /// Shows a tray notification balloon (no-op if disabled by settings).
    /// </summary>
    /// <param name="title">The title of the notification.</param>
    /// <param name="message">The message body of the notification.</param>
    /// <param name="kind">The kind of notification (info, warning, error).</param>
    /// <param name="timeoutMs">Alert timeout in milliseconds (default 5000ms).</param>
    void Show(string title, string message, TrayIconKind kind = TrayIconKind.Info, int timeoutMs = 5000);


    /// <summary>
    /// Shows an informational tray notification.
    /// </summary>
    /// <param name="title">The title of the notification.</param>
    /// <param name="message">The message body of the notification.</param>
    /// <param name="timeoutMs">Alert timeout in milliseconds (default 5000ms).</param>
    void Info(string title, string message, int timeoutMs = 5000);

    /// <summary>
    /// Shows a warning tray notification.
    /// </summary>
    /// <param name="title">The title of the notification.</param>
    /// <param name="message">The message body of the notification.</param>
    /// <param name="timeoutMs">Alert timeout in milliseconds (default 5000ms).</param>
    void Warn(string title, string message, int timeoutMs = 5000);

    /// <summary>
    /// Shows an error tray notification.
    /// </summary>
    /// <param name="title">The title of the notification.</param>
    /// <param name="message">The message body of the notification.</param>
    /// <param name="timeoutMs">Alert timeout in milliseconds (default 5000ms).</param>
    void Error(string title, string message, int timeoutMs = 5000);
}
