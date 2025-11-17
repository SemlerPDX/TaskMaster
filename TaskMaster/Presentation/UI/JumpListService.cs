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

using System.IO;
using System.Windows.Shell;

using TaskMaster.Application;
using TaskMaster.Presentation.Localization;
using TaskMaster.Services.Logging;

namespace TaskMaster.Presentation.UI;

/// <summary>
/// Declares and applies the application's Jump List (taskbar right-click menu).
/// </summary>
internal static class JumpListService
{
    private const string RelocateTaskTitleKey = "JumpList.RelocateTask.Title";
    private const string RelocateTaskDescriptionKey = "JumpList.RelocateTask.Description";

    private const string TaskSchedulerTaskTitleKey = "JumpList.TaskSchedulerTask.Title";
    private const string TaskSchedulerTaskDescriptionKey = "JumpList.TaskSchedulerTask.Description";

    private const string DefaultRelocateTitle = "Relocate to Primary Display";
    private const string DefaultRelocateDescription = "Restore TaskMaster and center it on the primary display.";
    private const string DefaultMmcTitle = "Windows Task Scheduler";
    private const string DefaultMmcDescription = "Open Windows Task Scheduler.";

    /// <summary>
    /// Applies the default Jump List for TaskMaster.
    /// </summary>
    /// <param name="localization">The localization service for retrieving localized strings.</param>
    public static void ApplyDefaultJumpList(ILocalizationService localization)
    {
        string appExePath = AppInfo.Location;

        if (string.IsNullOrWhiteSpace(appExePath) || !File.Exists(appExePath))
        {
            Log.Warn("JumpListService: Cannot apply Jump List because application executable path is invalid.");
            return;
        }

        var jumpList = new JumpList
        {
            ShowFrequentCategory = false,
            ShowRecentCategory = false
        };

        var taskSchedulerTitle = localization.GetString(TaskSchedulerTaskTitleKey);
        var taskSchedulerDescription = localization.GetString(TaskSchedulerTaskDescriptionKey);
        jumpList.JumpItems.Add(CreateTaskSchedulerTask(taskSchedulerTitle, taskSchedulerDescription));

        var relocateTitle = localization.GetString(RelocateTaskTitleKey);
        var relocateDescription = localization.GetString(RelocateTaskDescriptionKey);
        jumpList.JumpItems.Add(CreateRelocateTask(appExePath, relocateTitle, relocateDescription));

        JumpList.SetJumpList(System.Windows.Application.Current, jumpList);
    }


    private static JumpTask CreateRelocateTask(string appExePath, string? title, string? description)
    {
        // TaskMaster handles second-instance restore/relocate.
        return new JumpTask
        {
            Title = !string.IsNullOrEmpty(title) ? title : DefaultRelocateTitle,
            Description = !string.IsNullOrEmpty(description) ? description : DefaultRelocateDescription,
            ApplicationPath = appExePath,
            Arguments = string.Empty,
            IconResourcePath = appExePath,
            IconResourceIndex = 0
        };
    }

    private static JumpTask CreateTaskSchedulerTask(string? title, string? description)
    {
        // Prefer launching MMC with the Task Scheduler snap-in.
        string sys = Environment.GetFolderPath(Environment.SpecialFolder.System);
        string mmcPath = Path.Combine(sys, "mmc.exe");
        string taskSchdMsc = Path.Combine(sys, "taskschd.msc");
        string taskSchdDll = Path.Combine(sys, "taskschd.dll");

        bool hasMmc = File.Exists(mmcPath);
        bool hasMsc = File.Exists(taskSchdMsc);
        bool hasDll = File.Exists(taskSchdDll);

        // Fallbacks: if MMC isn't present for some reason, try launching the .msc directly.
        string appPath = hasMmc ? mmcPath : (hasMsc ? taskSchdMsc : mmcPath);
        string args = hasMmc && hasMsc ? taskSchdMsc : string.Empty;
        string iconPath = hasDll ? taskSchdDll : (hasMsc ? taskSchdMsc : mmcPath);

        return new JumpTask
        {
            Title = !string.IsNullOrEmpty(title) ? title : DefaultMmcTitle,
            Description = !string.IsNullOrEmpty(description) ? description : DefaultMmcDescription,
            ApplicationPath = appPath,
            Arguments = args,
            IconResourcePath = iconPath,
            IconResourceIndex = 0
        };
    }
}
