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
/// Service that manages application themes.
/// </summary>
public interface IThemeService
{
    /// <summary>
    /// Gets the current theme name.<br/>
    /// <br/>
    /// See <see cref="ThemeCatalog.DefaultTheme"/> for default theme.
    /// </summary>
    string CurrentTheme { get; }

    /// <summary>
    /// Event raised when the theme is changed.
    /// </summary>
    event EventHandler? ThemeChanged;

    /// <summary>
    /// Applies the specified theme by loading the corresponding resource dictionary.<br/>
    /// <br/>
    /// See <see cref="ThemeCatalog.ThemeOptions"/> for base options.
    /// </summary>
    /// <param name="themeName">The theme name. If null or empty, defaults to <see cref="ThemeCatalog.DefaultTheme"/>.</param>
    void ApplyTheme(string themeName);

    /// <summary>
    /// Returns true if the OS prefers Light for apps.<br/>
    /// <br/>
    /// Defaults to <see langword="true"/> (<see cref="ThemeCatalog.LightTheme"/>)
    /// or <see langword="false"/> (<see cref="ThemeCatalog.DarkTheme"/>)<br/>
    /// depending on <see cref="ThemeCatalog.DefaultTheme"/> if undetermined.
    /// </summary>
    /// <returns><see langword="True"/> if OS theme is Light; <see langword="false"/> if otherwise.</returns>
    bool IsOsLightTheme();
}
