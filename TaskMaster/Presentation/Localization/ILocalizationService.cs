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

namespace TaskMaster.Presentation.Localization;

/// <summary>
/// Service for managing localization and resource strings.
/// </summary>
public interface ILocalizationService
{
    /// <summary>
    /// Gets the current culture name (i.e. "en-US").<br/>
    /// Default is "en-US".
    /// </summary>
    string CurrentCulture { get; }

    /// <summary>
    /// Gets the current culture code (i.e. "en").<br/>
    /// Default is "en".
    /// </summary>
    string CurrentCultureCode { get; }

    /// <summary>
    /// Event raised when the language is changed.
    /// </summary>
    event EventHandler? LanguageChanged;

    /// <summary>
    /// Applies the specified culture by loading the corresponding resource dictionary.
    /// </summary>
    /// <param name="cultureName">The culture name (e.g., "en-US"). If null or empty, defaults to "en-US".</param>
    void ApplyCulture(string cultureName);

    /// <summary>
    /// Gets the localized string for the specified key.
    /// </summary>
    /// <param name="key">The resource key.</param>
    /// <returns>The localized string.</returns>
    string GetString(string key);

    /// <summary>
    /// Gets the localized string for the specified key.
    /// </summary>
    /// <param name="key">The resource key.</param>
    /// <param name="fallback">The fallback string if the key is not found. If null, the key itself is returned.</param>
    /// <returns>The localized string, or the fallback (if provided) if not found.</returns>
    string GetString(string key, string? fallback = null);
}
