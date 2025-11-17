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
/// Provides feature-specific services used in various application features.
/// </summary>
public sealed class FeatureServices : IFeatureServices
{
    /// <summary>
    /// Gets the localization service.
    /// </summary>
    public ILocalizationService Localization { get { return _common.Localization; } }

    /// <summary>
    /// Gets the settings store.
    /// </summary>
    public ISettingsStore Settings { get { return _common.Settings; } }

    /// <summary>
    /// Gets the configuration store.
    /// </summary>
    public IConfigStore Config { get { return _common.Config; } }

    /// <summary>
    /// Gets the application visibility service.
    /// </summary>
    public IAppVisibility Visibility { get { return _common.Visibility; } }

    /// <summary>
    /// Gets the system tray notification service.
    /// </summary>
    public ITrayNotifications Tray { get { return _common.Tray; } }

    /// <summary>
    /// Gets the history log service for the task launcher feature.
    /// </summary>
    public IHistoryLog LauncherHistory { get; }

    /// <summary>
    /// Gets the history log service for the task killer feature.
    /// </summary>
    public IHistoryLog KillerHistory { get; }

    /// <summary>
    /// The common application services.
    /// </summary>
    private readonly ICommonServices _common;

    /// <summary>
    /// Initializes a new instance of the <see cref="FeatureServices"/> class.
    /// </summary>
    /// <param name="common">The common application services.</param>
    /// <param name="launcherHistory">The history log service for the task launcher feature.</param>
    /// <param name="killerHistory">The history log service for the task killer feature.</param>
    public FeatureServices(
        ICommonServices common,
        IHistoryLog launcherHistory,
        IHistoryLog killerHistory)
    {
        _common = common;
        LauncherHistory = launcherHistory;
        KillerHistory = killerHistory;
    }
}
