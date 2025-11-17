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

using TaskMaster.Application.Composition;
using TaskMaster.Presentation.UI;
using TaskMaster.Services.Logging;

namespace TaskMaster.Services;

/// <summary>
/// Provides localized information services for tray notifications and app history logs.
/// </summary>
internal static class InfoService
{
    /// <summary>
    /// Data for a localized tray notification.
    /// </summary>
    public sealed class TrayData
    {
        /// <summary>
        /// The localization resource dictionary key for the title.
        /// </summary>
        public required string TitleKey { get; init; }

        /// <summary>
        /// The default title if localization resource dictionary key is not found.
        /// </summary>
        public string DefaultTitle { get; init; } = string.Empty;

        /// <summary>
        /// The localization resource dictionary key for the message.
        /// </summary>
        public required string MessageKey { get; init; }

        /// <summary>
        /// Optional default message if localization resource dictionary key is not found.
        /// </summary>
        public string? DefaultMessage { get; init; }

        /// <summary>
        /// The type of tray notification message.<br/>
        /// Defaults to Info when not provided.
        /// </summary>
        public TrayIconKind MessageType { get; init; } = TrayIconKind.Info;
    }


    /// <summary>
    /// Shows a tray notification with localized title and message.
    /// </summary>
    /// <param name="features">The feature services.</param>
    /// <param name="trayData">The tray notification data.</param>
    public static void TrayNotification(IFeatureServices features, TrayData trayData) =>
        TrayNotification(features, trayData, []);

    /// <summary>
    /// Shows a tray notification with localized title and message with arguments for message formatting.
    /// </summary>
    /// <param name="features">The feature services.</param>
    /// <param name="trayData">The tray notification data.</param>
    /// <param name="args">Arguments for formatting the message.</param>
    public static void TrayNotification(IFeatureServices features, TrayData trayData, params object[] args)
    {
        try
        {
            ArgumentException.ThrowIfNullOrEmpty(trayData.TitleKey, nameof(trayData.TitleKey));
            ArgumentException.ThrowIfNullOrEmpty(trayData.MessageKey, nameof(trayData.MessageKey));

            var localizedTitle = features.Localization.GetString(trayData.TitleKey, trayData.DefaultTitle);
            var localizedMessage = features.Localization.GetString(trayData.MessageKey, trayData.DefaultMessage);
            var message = (args.Length > 0) ? string.Format(localizedMessage, args) : localizedMessage;
            switch (trayData.MessageType)
            {
                case TrayIconKind.Info: features.Tray.Info(localizedTitle, message); break;
                case TrayIconKind.Warning: features.Tray.Warn(localizedTitle, message); break;
                case TrayIconKind.Error: features.Tray.Error(localizedTitle, message); break;
                default: break;
            }
        }
        catch (ArgumentException argEx)
        {
            Log.Error(argEx, "Tray Notification Argument Error");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Tray Notification Error");
        }
    }


    /// <summary>
    /// Logs a message to the task launcher history log.
    /// </summary>
    /// <param name="features">The feature services.</param>
    /// <param name="key">The localization resource dictionary key.</param>
    /// <param name="defaultMsg">Optional default message if localization resource dictionary key is not found.</param>
    public static void LogLauncherHistory(IFeatureServices features, string key, string? defaultMsg = null) =>
        LogLauncherHistory(features, key, defaultMsg, []);

    /// <summary>
    /// Logs a message to the task launcher history log with arguments for message formatting.
    /// </summary>
    /// <param name="features">The feature services.</param>
    /// <param name="key">The localization resource dictionary key.</param>
    /// <param name="defaultMsg">Optional default message if localization resource dictionary key is not found.</param>
    /// <param name="args">Arguments for formatting the message.</param>
    public static void LogLauncherHistory(IFeatureServices features, string key, string? defaultMsg = null, params object[] args)
    {
        var localizedMessage = features.Localization.GetString(key, defaultMsg);
        var message = (args.Length > 0) ? string.Format(localizedMessage, args) : localizedMessage;
        features.LauncherHistory.Append(message);
    }


    /// <summary>
    /// Logs a message to the task killer history log.
    /// </summary>
    /// <param name="features">The feature services.</param>
    /// <param name="key">The localization resource dictionary key.</param>
    /// <param name="defaultMsg">Optional default message if localization resource dictionary key is not found.</param>
    public static void LogKillerHistory(IFeatureServices features, string key, string? defaultMsg = null) =>
        LogKillerHistory(features, key, defaultMsg, []);

    /// <summary>
    /// Logs a message to the task killer history log with arguments for message formatting.
    /// </summary>
    /// <param name="features">The feature services.</param>
    /// <param name="key">The localization resource dictionary key.</param>
    /// <param name="defaultMsg">Optional default message if localization resource dictionary key is not found.</param>
    /// <param name="args">Arguments for formatting the message.</param>
    public static void LogKillerHistory(IFeatureServices features, string key, string? defaultMsg = null, params object[] args)
    {
        var localizedMessage = features.Localization.GetString(key, defaultMsg);
        var message = (args.Length > 0) ? string.Format(localizedMessage, args) : localizedMessage;
        features.KillerHistory.Append(message);
    }

}
