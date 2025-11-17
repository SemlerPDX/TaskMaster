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

using TaskMaster.Infrastructure.Persistence;

namespace TaskMaster.Presentation.UI;

/// <summary>
/// Centralized, settings-aware tray notification service.
/// </summary>
internal sealed class TrayNotificationService : ITrayNotifications, IDisposable
{
    private readonly ISettingsStore _settings;
    private readonly IAppVisibility? _visibility;

    private NotifyIcon? _icon;

    /// <summary>
    /// Gets the current effective enablement (reflects settings).
    /// </summary>
    public bool IsEnabled { get; private set; }



    /// <summary>
    /// Initializes a new instance of the TrayNotificationService class.
    /// </summary>
    /// <param name="settings">The settings store to read notification settings from.</param>
    /// <param name="visibility">The application visibility service (optional).</param>
    /// <exception cref="ArgumentNullException">Thrown if settings is null.</exception>
    public TrayNotificationService(ISettingsStore settings, IAppVisibility? visibility = null)
    {
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _visibility = visibility;

        IsEnabled = _settings.Current.ShowTrayNotifications && _settings.Current.MinimizeToTray;
        _settings.SettingsApplied += OnSettingsApplied;
    }



    /// <summary>
    /// Attaches the view-owned NotifyIcon (call from MainWindow).
    /// </summary>
    /// <param name="notifyIcon">The NotifyIcon to attach.</param>
    /// <exception cref="ArgumentNullException">Thrown if notifyIcon is null.</exception>
    public void Attach(NotifyIcon notifyIcon)
    {
        _icon = notifyIcon ?? throw new ArgumentNullException(nameof(notifyIcon));
    }

    /// <summary>
    /// Detaches the previously-attached NotifyIcon.
    /// </summary>
    public void Detach()
    {
        _icon = null;
    }

    /// <summary>
    /// Shows a tray notification balloon (no-op if disabled by settings).
    /// </summary>
    /// <param name="title">The title of the notification.</param>
    /// <param name="message">The message body of the notification.</param>
    /// <param name="kind">The kind of notification (info, warning, error).</param>
    /// <param name="timeoutMs">Alert timeout in milliseconds (default 5000ms).</param>
    public void Show(string title, string message, TrayIconKind kind = TrayIconKind.Info, int timeoutMs = 5000)
    {
        if (!IsEnabled || _icon == null)
        {
            return;
        }

        if (_visibility != null && _visibility.IsVisible && !_visibility.IsMinimized && !_visibility.IsInTray)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(message))
        {
            return;
        }

        // Marshal to WPF dispatcher to keep UI-thread semantics aligned
        var disp = System.Windows.Application.Current?.Dispatcher;
        void show()
        {
            _icon.Visible = true;
            _icon.BalloonTipTitle = title;
            _icon.BalloonTipText = message;
            _icon.BalloonTipIcon = kind switch
            {
                TrayIconKind.Warning => ToolTipIcon.Warning,
                TrayIconKind.Error => ToolTipIcon.Error,
                TrayIconKind.None => ToolTipIcon.None,
                _ => ToolTipIcon.Info
            };

            _icon.ShowBalloonTip(timeoutMs);
        }

        if (disp != null && !disp.CheckAccess())
        {
            disp.BeginInvoke((Action)show);
        }
        else
        {
            show();
        }
    }

    /// <summary>
    /// Shows an informational tray notification.
    /// </summary>
    /// <param name="title">The title of the notification.</param>
    /// <param name="message">The message body of the notification.</param>
    /// <param name="timeoutMs">Alert timeout in milliseconds (default 5000ms).</param>
    public void Info(string title, string message, int timeoutMs = 5000) => Show(title, message, TrayIconKind.Info, timeoutMs);

    /// <summary>
    /// Shows a warning tray notification.
    /// </summary>
    /// <param name="title">The title of the notification.</param>
    /// <param name="message">The message body of the notification.</param>
    /// <param name="timeoutMs">Alert timeout in milliseconds (default 5000ms).</param>
    public void Warn(string title, string message, int timeoutMs = 5000) => Show(title, message, TrayIconKind.Warning, timeoutMs);

    /// <summary>
    /// Shows an error tray notification.
    /// </summary>
    /// <param name="title">The title of the notification.</param>
    /// <param name="message">The message body of the notification.</param>
    /// <param name="timeoutMs">Alert timeout in milliseconds (default 5000ms).</param>
    public void Error(string title, string message, int timeoutMs = 5000) => Show(title, message, TrayIconKind.Error, timeoutMs);


    private void OnSettingsApplied(object? s, EventArgs e)
    {
        IsEnabled = _settings.Current.ShowTrayNotifications && _settings.Current.MinimizeToTray;
    }


    /// <summary>
    /// Cleans up event handlers and disposes resources.
    /// </summary>
    public void Dispose()
    {
        _settings.SettingsApplied -= OnSettingsApplied;
        // We do NOT own _icon here; MainWindow will dispose it.
        _icon = null;
    }
}
