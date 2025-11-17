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

using System.Windows;

using Microsoft.Win32;

namespace TaskMaster.Presentation.UI;

/// <summary>
/// Service that manages application color palette themes.
/// </summary>
public sealed class ThemeService : IThemeService
{
    private const string ResourcePathFormat = "/Resources/Themes/{0}/Theme.xaml";
    private const string OsThemeRegPath = @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize";
    private const string AppsUseLightThemeValue = "AppsUseLightTheme";

    private ResourceDictionary? _current;


    /// <summary>
    /// Gets the current theme name.<br/>
    /// <br/>
    /// See <see cref="ThemeCatalog.DefaultTheme"/> for default theme.
    /// </summary>
    public string CurrentTheme { get; private set; } = ThemeCatalog.DefaultTheme;

    /// <summary>
    /// Event raised when the theme is changed.
    /// </summary>
    public event EventHandler? ThemeChanged;


    /// <summary>
    /// Applies the specified theme by loading the corresponding resource dictionary.<br/>
    /// <br/>
    /// See <see cref="ThemeCatalog.ThemeOptions"/> for base options.
    /// </summary>
    /// <param name="themeName">The theme name. If null or empty, defaults to <see cref="ThemeCatalog.DefaultTheme"/>.</param>
    public void ApplyTheme(string themeName)
    {
        if (string.IsNullOrWhiteSpace(themeName))
        {
            themeName = ThemeCatalog.DefaultTheme;
        }

        var path = string.Format(ResourcePathFormat, themeName);
        var dict = new ResourceDictionary { Source = new Uri(path, UriKind.Relative) };

        var app = System.Windows.Application.Current;
        if (app == null)
        {
            return;
        }

        if (_current != null)
        {
            app.Resources.MergedDictionaries.Remove(_current);
        }

        app.Resources.MergedDictionaries.Add(dict);
        _current = dict;
        CurrentTheme = themeName;

        ThemeChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Returns true if the OS prefers Light for apps.<br/>
    /// <br/>
    /// Defaults to <see langword="true"/> (<see cref="ThemeCatalog.LightTheme"/>)
    /// or <see langword="false"/> (<see cref="ThemeCatalog.DarkTheme"/>)<br/>
    /// depending on <see cref="ThemeCatalog.DefaultTheme"/> if undetermined.
    /// </summary>
    /// <returns><see langword="True"/> if OS theme is Light; <see langword="false"/> if otherwise.</returns>
    public bool IsOsLightTheme()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(OsThemeRegPath, writable: false);
            var raw = key?.GetValue(AppsUseLightThemeValue);
            if (raw != null)
            {
                var n = Convert.ToInt32(raw);
                return n != 0;
            }
        }
        catch
        {
            // fall through fail-safe to default theme
        }

        return string.Equals(ThemeCatalog.DefaultTheme, ThemeCatalog.LightTheme, StringComparison.OrdinalIgnoreCase);
    }
}
