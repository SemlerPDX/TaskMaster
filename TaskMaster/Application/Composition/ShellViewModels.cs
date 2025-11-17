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
public sealed class ShellViewModels : IShellViewModels
{
    /// <summary>
    /// Gets the launcher tab view model.
    /// </summary>
    public LauncherTabViewModel Launcher { get; }

    /// <summary>
    /// Gets the killer tab view model.
    /// </summary>
    public KillerTabViewModel Killer { get; }

    /// <summary>
    /// Gets the settings tab view model.
    /// </summary>
    public SettingsTabViewModel Settings { get; }


    /// <summary>
    /// Gets the launcher dialog view model.
    /// </summary>
    public LauncherDialogViewModel LauncherDialog { get; }

    /// <summary>
    /// Gets the modal dialog view model.
    /// </summary>
    public ModalDialogViewModel Modal { get; }

    /// <summary>
    /// Gets the culture dialog view model.
    /// </summary>
    public CultureDialogViewModel Culture { get; }

    /// <summary>
    /// Gets the about dialog view model.
    /// </summary>
    public AboutDialogViewModel About { get; }

    /// <summary>
    /// Gets the license dialog view model.
    /// </summary>
    public LicenseDialogViewModel License { get; }

    /// <summary>
    /// Gets the help dialog view model.
    /// </summary>
    public HelpDialogViewModel Help { get; }

    /// <summary>
    /// Gets the support dialog view model.
    /// </summary>
    public SupportDialogViewModel Support { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ShellViewModels"/> class.
    /// </summary>
    /// <param name="launcher">The task launcher tab view model.</param>
    /// <param name="killer">The task killer tab view model.</param>
    /// <param name="settings">The settings tab view model.</param>
    /// <param name="launcherDialog">The task launcher dialog view model.</param>
    /// <param name="modal">The modal dialog view model.</param>
    /// <param name="culture">The culture dialog view model.</param>
    /// <param name="about">The about dialog view model.</param>
    /// <param name="license">The license dialog view model.</param>
    /// <param name="help">The help dialog view model.</param>
    /// <param name="support">The support dialog view model.</param>
    public ShellViewModels(
        LauncherTabViewModel launcher,
        KillerTabViewModel killer,
        SettingsTabViewModel settings,
        LauncherDialogViewModel launcherDialog,
        ModalDialogViewModel modal,
        CultureDialogViewModel culture,
        AboutDialogViewModel about,
        LicenseDialogViewModel license,
        HelpDialogViewModel help,
        SupportDialogViewModel support)
    {
        Launcher = launcher;
        Killer = killer;
        Settings = settings;

        LauncherDialog = launcherDialog;
        Modal = modal;
        Culture = culture;
        About = about;
        License = license;
        Help = help;
        Support = support;
    }
}
