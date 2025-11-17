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
public sealed class ShellServices : IShellServices
{
    // Configuration / UX
    /// <summary>
    /// Service for managing application themes.
    /// </summary>
    public IThemeService Theme { get; }

    /// <summary>
    /// Service for managing application styles.
    /// </summary>
    public IStyleService Style { get; }


    // Process layer
    /// <summary>
    /// Service for monitoring running processes.
    /// </summary>
    public IProcessMonitorService Monitor { get; }

    /// <summary>
    /// Store for process exclusions.
    /// </summary>
    public IProcessExclusionsStore ExclusionsStore { get; }

    /// <summary>
    /// Policy for determining which processes are excluded.
    /// </summary>
    public IProcessExclusionPolicy Exclusions { get; }

    /// <summary>
    /// Policy for determining unique process entries.
    /// </summary>
    public IUniqueEntryPolicy UniqueEntryPolicy { get; }

    /// <summary>
    /// Adapter for accessing the list of running processes.
    /// </summary>
    public ProcessListAdapter Running { get; }

    // Features
    /// <summary>
    /// Service for terminating tasks/processes.
    /// </summary>
    public TaskKiller TaskKiller { get; }

    /// <summary>
    /// Service for launching tasks/processes.
    /// </summary>
    public TaskLauncher TaskLauncher { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ShellServices"/> class.
    /// </summary>
    /// <param name="theme">The theme service.</param>
    /// <param name="style">The style service.</param>
    /// <param name="monitor">The process monitor service.</param>
    /// <param name="exclusionsStore">The process exclusions store.</param>
    /// <param name="exclusions">The process exclusion policy.</param>
    /// <param name="uniqueEntryPolicy">The unique entry policy.</param>
    /// <param name="running">The running processes adapter.</param>
    /// <param name="taskKiller">The task killer service.</param>
    /// <param name="taskLauncher">The task launcher service.</param>
    public ShellServices(
        IThemeService theme,
        IStyleService style,
        IProcessMonitorService monitor,
        IProcessExclusionsStore exclusionsStore,
        IProcessExclusionPolicy exclusions,
        IUniqueEntryPolicy uniqueEntryPolicy,
        ProcessListAdapter running,
        TaskKiller taskKiller,
        TaskLauncher taskLauncher)
    {
        Theme = theme;
        Style = style;

        Monitor = monitor;
        ExclusionsStore = exclusionsStore;
        Exclusions = exclusions;
        UniqueEntryPolicy = uniqueEntryPolicy;
        Running = running;

        TaskKiller = taskKiller;
        TaskLauncher = taskLauncher;
    }
}
