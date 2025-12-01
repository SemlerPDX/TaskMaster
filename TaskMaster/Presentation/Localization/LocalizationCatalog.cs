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
/// Represents a language item with culture code, display name, and flag path.
/// </summary>
public sealed record LangItem
{
    /// <summary>
    /// Gets or sets the culture code (e.g., "en-US").
    /// </summary>
    public string Culture { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the display name of the language.
    /// </summary>
    public string Display { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the path to the flag image representing the language.
    /// </summary>
    public string FlagPath { get; set; } = string.Empty;
}

/// <summary>
/// Provides a catalog of supported localization languages.
/// </summary>
public static class LocalizationCatalog
{
    private const string FlagsPath = @"/Resources/Images/flags/";
    private const string FlagsFormat = ".png";

    /// <summary>
    /// Gets the list of supported languages.
    /// </summary>
    public static IReadOnlyList<LangItem> Languages { get; } =
    [
        // English (Default)
        new LangItem { Culture = "en-US", Display = "English (US)",     FlagPath = FlagsPath + "us" + FlagsFormat },
        // Spanish
        new LangItem { Culture = "es-ES", Display = "Español (España)", FlagPath = FlagsPath + "es" + FlagsFormat },
        // Chinese (Simplified)
        new LangItem { Culture = "zh-CN", Display = "中文（简体）", FlagPath = FlagsPath + "cn" + FlagsFormat },
        // -- Other languages commented out for future use--
        //// German
        //new LangItem { Culture = "de-DE", Display = "Deutsch (Deutschland)", FlagPath = FlagsPath + "de" + FlagsFormat },
        //// French
        //new LangItem { Culture = "fr-FR", Display = "Français (France)", FlagPath = FlagsPath + "fr" + FlagsFormat },
        //// Australian
        //new LangItem { Culture = "en-AU", Display = "English (Australia)", FlagPath = FlagsPath + "au" + FlagsFormat },
        //// Polish
        //new LangItem { Culture = "pl-PL", Display = "Polski (Polska)", FlagPath = FlagsPath + "pl" + FlagsFormat },
        //// Danish
        //new LangItem { Culture = "da-DK", Display = "Dansk (Danmark)", FlagPath = FlagsPath + "dk" + FlagsFormat },
        //// Swedish
        //new LangItem { Culture = "sv-SE", Display = "Svenska (Sverige)", FlagPath = FlagsPath + "se" + FlagsFormat },
        //// Finnish
        //new LangItem { Culture = "fi-FI", Display = "Suomi (Suomi)", FlagPath = FlagsPath + "fi" + FlagsFormat },
        //// Italian
        //new LangItem { Culture = "it-IT", Display = "Italiano (Italia)", FlagPath = FlagsPath + "it" + FlagsFormat },
        //// Greek
        //new LangItem { Culture = "el-GR", Display = "Ελληνικά (Ελλάδα)", FlagPath = FlagsPath + "gr" + FlagsFormat },
        //// Turkish
        //new LangItem { Culture = "tr-TR", Display = "Türkçe (Türkiye)", FlagPath = FlagsPath + "tr" + FlagsFormat },
        //// Japanese
        //new LangItem { Culture = "ja-JP", Display = "日本語（日本）", FlagPath = FlagsPath + "jp" + FlagsFormat }
    ];
}
