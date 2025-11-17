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

using TaskMaster.Presentation.UI;

namespace TaskMaster.Application.Composition;

/// <summary>
/// Provides feature-specific services used in various application features.
/// </summary>
public interface IFeatureServices : ICommonServices
{
    /// <summary>
    /// Gets the history log service for the task launcher feature.
    /// </summary>
    IHistoryLog LauncherHistory { get; }

    /// <summary>
    /// Gets the history log service for the task killer feature.
    /// </summary>
    IHistoryLog KillerHistory { get; }
}
