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
using TaskMaster.Presentation.Localization;
using TaskMaster.Presentation.UI;

namespace TaskMaster.Application.Composition;

/// <summary>
/// Provides common services used throughout the application.
/// </summary>
public interface ICommonServices
{
    /// <summary>
    /// Gets the localization service.
    /// </summary>
    ILocalizationService Localization { get; }

    /// <summary>
    /// Gets the settings store.
    /// </summary>
    ISettingsStore Settings { get; }

    /// <summary>
    /// Gets the configuration store.
    /// </summary>
    IConfigStore Config { get; }

    /// <summary>
    /// Gets the application visibility service.
    /// </summary>
    IAppVisibility Visibility { get; }

    /// <summary>
    /// Gets the system tray notification service.
    /// </summary>
    ITrayNotifications Tray { get; }
}
