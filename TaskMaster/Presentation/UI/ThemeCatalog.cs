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

namespace TaskMaster.Presentation.UI;

/// <summary>
/// Catalog of available application color palette themes.
/// </summary>
internal static class ThemeCatalog
{
    /// <summary>
    /// Default application color palette theme.
    /// </summary>
    public const string DefaultTheme = "Dark";

    /// <summary>
    /// Dark theme option.
    /// </summary>
    public const string DarkTheme = "Dark";

    /// <summary>
    /// Light theme option.
    /// </summary>
    public const string LightTheme = "Light";

    /// <summary>
    /// Auto theme option.<br/>
    /// For selecting theme based on system settings.
    /// </summary>
    public const string AutoTheme = "Auto";

    /// <summary>
    /// Available application color palette themes.<br/>
    /// Allows "Auto" to select theme based on system settings.
    /// </summary>
    public static IReadOnlyList<string> ThemeOptions { get; } =
    [
        DarkTheme,
        LightTheme,
        AutoTheme
    ];
}
