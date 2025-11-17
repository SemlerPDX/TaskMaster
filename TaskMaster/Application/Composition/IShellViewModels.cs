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

using TaskMaster.ViewModel;
using TaskMaster.ViewModel.Dialog;

namespace TaskMaster.Application.Composition;

/// <summary>
/// DTO providing access to the shell's view models.
/// </summary>
public interface IShellViewModels
{
    /// <summary>
    /// Gets the launcher tab view model.
    /// </summary>
    LauncherTabViewModel Launcher { get; }

    /// <summary>
    /// Gets the killer tab view model.
    /// </summary>
    KillerTabViewModel Killer { get; }

    /// <summary>
    /// Gets the settings tab view model.
    /// </summary>
    SettingsTabViewModel Settings { get; }


    /// <summary>
    /// Gets the launcher dialog view model.
    /// </summary>
    LauncherDialogViewModel LauncherDialog { get; }

    /// <summary>
    /// Gets the modal dialog view model.
    /// </summary>
    ModalDialogViewModel Modal { get; }

    /// <summary>
    /// Gets the culture dialog view model.
    /// </summary>
    CultureDialogViewModel Culture { get; }

    /// <summary>
    /// Gets the about dialog view model.
    /// </summary>
    AboutDialogViewModel About { get; }

    /// <summary>
    /// Gets the license dialog view model.
    /// </summary>
    LicenseDialogViewModel License { get; }

    /// <summary>
    /// Gets the help dialog view model.
    /// </summary>
    HelpDialogViewModel Help { get; }

    /// <summary>
    /// Gets the support dialog view model.
    /// </summary>
    SupportDialogViewModel Support { get; }

}
