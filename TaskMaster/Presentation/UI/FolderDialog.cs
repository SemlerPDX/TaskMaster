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

namespace TaskMaster.Presentation.UI;

/// <summary>
/// Service for selecting an executable file via a WPF file browser dialog.<br/>
/// Allows specifying an initial directory and custom title (for localization).
/// </summary>
internal static class FolderDialog
{
    private const string DefaultTitle = "Select Application";
    private const string ExecutableFilter =
        "Executables (*.exe;*.bat;*.cmd;*.com)|*.exe;*.bat;*.cmd;*.com|All Files (*.*)|*.*";

    /// <summary>
    /// Opens a file dialog for selecting an executable file, starting in Program Files by default.
    /// </summary>
    /// <param name="selectedPath">The selected folder path, if any, to output from user selection.</param>
    /// <param name="title">The dialog title, if any (for localization). Default is "Select Application".</param>
    /// <returns><see langword="True"/> if a file was selected; <see langword="false"/> if canceled.</returns>
    public static bool TryOpenFileDialog(out string selectedPath, string? title = null) { return TryOpenFileDialog(null, out selectedPath, title); }

    /// <summary>
    /// Opens a file dialog for selecting an executable file, starting in Program Files by default.
    /// </summary>
    /// <param name="initialDir">The initial directory to start in, if any; otherwise defaults to Program Files.</param>
    /// <param name="selectedPath">The selected folder path, if any, to output from user selection.</param>
    /// <param name="title">The dialog title, if any (for localization). Default is "Select Application".</param>
    /// <returns><see langword="True"/> if a file was selected; <see langword="false"/> if canceled.</returns>
    public static bool TryOpenFileDialog(string? initialDir, out string selectedPath, string? title = null)
    {
        selectedPath = string.Empty;

        // Reasonable default if none/invalid supplied
        var startDir = Directory.Exists(initialDir ?? string.Empty)
            ? initialDir!
            : Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);

        var dlg = new Microsoft.Win32.OpenFileDialog
        {
            Title = string.IsNullOrEmpty(title) ? DefaultTitle : title,
            InitialDirectory = startDir,
            Filter = ExecutableFilter,
            CheckFileExists = true,
            Multiselect = false,
            AddExtension = false,
            DereferenceLinks = true,
            ValidateNames = true
        };

        bool? ok = dlg.ShowDialog(System.Windows.Application.Current?.MainWindow);
        if (ok == true)
        {
            selectedPath = dlg.FileName ?? string.Empty;
            return !string.IsNullOrWhiteSpace(selectedPath);
        }

        return false;
    }
}
