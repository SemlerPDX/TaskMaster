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

using TaskMaster.Application.Features;
using TaskMaster.Domain.Policies;
using TaskMaster.Infrastructure.Persistence;
using TaskMaster.Infrastructure.Processes;
using TaskMaster.Presentation.UI;
using TaskMaster.ViewModel;

namespace TaskMaster.Application.Composition;

/// <summary>
/// Provides shell-wide services used throughout the application.
/// </summary>
public interface IShellServices
{
    // Configuration / UX
    /// <summary>
    /// Service for managing application themes.
    /// </summary>
    IThemeService Theme { get; }

    /// <summary>
    /// Service for managing application styles.
    /// </summary>
    IStyleService Style { get; }


    // Process layer
    /// <summary>
    /// Service for monitoring running processes.
    /// </summary>
    IProcessMonitorService Monitor { get; }

    /// <summary>
    /// Store for process exclusions.
    /// </summary>
    IProcessExclusionsStore ExclusionsStore { get; }

    /// <summary>
    /// Policy for determining which processes are excluded.
    /// </summary>
    IProcessExclusionPolicy Exclusions { get; }

    /// <summary>
    /// Policy for determining unique process entries.
    /// </summary>
    IUniqueEntryPolicy UniqueEntryPolicy { get; }

    /// <summary>
    /// Adapter for accessing the list of running processes.
    /// </summary>
    ProcessListAdapter Running { get; }


    // Features
    /// <summary>
    /// Service for terminating tasks/processes.
    /// </summary>
    TaskKiller TaskKiller { get; }

    /// <summary>
    /// Service for launching tasks/processes.
    /// </summary>
    TaskLauncher TaskLauncher { get; }
}
