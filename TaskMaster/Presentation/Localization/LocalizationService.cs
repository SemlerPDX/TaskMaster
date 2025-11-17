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


namespace TaskMaster.Presentation.Localization;

/// <summary>
/// Service for managing localization and resource strings.
/// </summary>
public sealed class LocalizationService : ILocalizationService
{
    private const string DefaultCulture = "en-US";
    private const string DefaultFallbackMsg = "[[MISSING STRING]] {0}";
    private const string ResourcePathFormat = "/Resources/Localization/Strings.{0}.xaml";

    private ResourceDictionary? _current;


    /// <summary>
    /// Gets the current culture name (i.e. "en-US").<br/>
    /// Default is "en-US".
    /// </summary>
    public string CurrentCulture { get; private set; } = DefaultCulture;

    /// <summary>
    /// Gets the current culture code (i.e. "en").<br/>
    /// Default is "en".
    /// </summary>
    public string CurrentCultureCode
    {
        get { return CurrentCulture.Split(['-'])[0]; }
    }

    /// <summary>
    /// Event raised when the language is changed.
    /// </summary>
    public event EventHandler? LanguageChanged;


    /// <summary>
    /// Applies the specified culture by loading the corresponding resource dictionary.
    /// </summary>
    /// <param name="cultureName">The culture name (i.e. "en-US"). If null or empty, defaults to "en-US".</param>
    public void ApplyCulture(string? cultureName)
    {
        if (string.IsNullOrWhiteSpace(cultureName))
        {
            cultureName = DefaultCulture;
        }

        var uriString = string.Format(ResourcePathFormat, cultureName);
        var dict = new ResourceDictionary
        {
            Source = new Uri(uriString, UriKind.Relative)
        };

        var app = System.Windows.Application.Current;
        if (app == null)
        {
            return;
        }

        // Remove previous
        if (_current != null)
        {
            app.Resources.MergedDictionaries.Remove(_current);
        }

        // Add new
        app.Resources.MergedDictionaries.Add(dict);
        _current = dict;
        CurrentCulture = cultureName;

        // TODO: Consider numeric localization for decimal inputs in SettingsTabView (??)
        //// Set framework language for number/date formatting in bindings
        //FrameworkElement.LanguageProperty.OverrideMetadata(
        //    typeof(FrameworkElement),
        //    new FrameworkPropertyMetadata(System.Windows.Markup.XmlLanguage.GetLanguage(new CultureInfo(cultureName).IetfLanguageTag)));

        LanguageChanged?.Invoke(this, EventArgs.Empty);
    }


    /// <summary>
    /// Gets the localized string for the specified key.
    /// </summary>
    /// <param name="key">The resource key.</param>
    /// <returns>The localized string.</returns>
    public string GetString(string key) => GetString(key, null);

    /// <summary>
    /// Gets the localized string for the specified key, with optional fallback default.
    /// </summary>
    /// <param name="key">The resource key.</param>
    /// <param name="fallback">The fallback string if the key is not found. If null, the key itself is returned.</param>
    /// <returns>The localized string, or the fallback (if provided) if not found.</returns>
    public string GetString(string key, string? fallback = null)
    {
        if (System.Windows.Application.Current?.TryFindResource(key) is string s)
        {
            return s.Replace(@"\n", "\n");
        }

        return fallback ?? string.Format(DefaultFallbackMsg, key);
    }
}

