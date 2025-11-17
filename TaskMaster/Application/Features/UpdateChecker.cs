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

using System.Net.Http;
using System.Text;
using System.Text.Json;

using TaskMaster.Services.Logging;

namespace TaskMaster.Application.Features;

/// <summary>
/// Service for checking application updates via a simple JSON manifest hosted on GitHub.
/// </summary>
public static class UpdateChecker
{
    private const string UpdateUrl = @"https://raw.githubusercontent.com/SemlerPDX/TaskMaster/refs/heads/master/version.json";
    private const string ErrorMessage = "Error occurred in UpdateChecker class.";


    // ---- JSON Model ----
    private sealed class UpdateManifest
    {
        /// <summary>
        /// Version string in "X.X.X" or "X.X.X.X" format.
        /// </summary>
        public string Version { get; set; } = string.Empty;

        /// <summary>
        /// Localized feature text keyed by culture code (i.e. "en-US", "es-ES").
        /// </summary>
        public Dictionary<string, string> Features { get; set; } = [];
    }


    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };


    // ---- HTTP Fetch ----
    private static readonly HttpClient Http;

    /// <summary>
    /// Static constructor to initialize HTTP client.
    /// </summary>
    static UpdateChecker()
    {
        Http = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(8)
        };

        // A friendly UA + JSON accept; could help (?) with some stubborn CDNs like GitHub
        var current = AppInfo.VersionString;
        Http.DefaultRequestHeaders.UserAgent.ParseAdd($"{AppInfo.Name}/{current}");
        Http.DefaultRequestHeaders.Accept.ParseAdd("application/json");
    }

    /// <summary>
    /// Checks the remote manifest for updates and returns a culture-specific
    /// feature string where possible.
    /// </summary>
    /// <param name="uiCulture">UI culture code (i.e. "en-US", "es-ES"). If <see langword="null"/>, the method<br/>
    /// falls back to neutral/English and finally any available text.</param>
    /// <returns>A string array containing the latest version and feature if an update is available,<br/>
    /// or the current version if no update is found; <see langword="null"/> on error.</returns>
    public static async Task<string[]?> CheckForUpdatesAsync(string? uiCulture)
    {
        string raw;

        // Download latest version manifest --
        try
        {
            raw = await Http.GetStringAsync(UpdateUrl).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Http Get " + ErrorMessage);
            return null;
        }

        if (string.IsNullOrWhiteSpace(raw))
        {
            Log.Error("Http Data " + ErrorMessage);
            return null;
        }

        // Parse manifest --
        UpdateManifest? manifest;
        try
        {
            manifest = JsonSerializer.Deserialize<UpdateManifest>(raw, JsonOptions);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Deserialize " + ErrorMessage);
            return null;
        }

        if (manifest == null)
        {
            Log.Error("Manifest null " + ErrorMessage);
            return null;
        }

        if (string.IsNullOrWhiteSpace(manifest.Version) ||
            manifest.Features == null ||
            manifest.Features.Count == 0)
        {
            Log.Error("Manifest Data " + ErrorMessage);
            return null;
        }


        // Compare current version with manifest --
        var currentVersion = AppInfo.Version;
        if (currentVersion == null)
        {
            Log.Error("Version Get " + ErrorMessage);
            return null;
        }

        var normalizedVersion = NormalizeVersion(manifest.Version);
        if (!Version.TryParse(normalizedVersion, out var latestVersion))
        {
            Log.Error("Version Parse " + ErrorMessage);
            return null;
        }

        bool isUpdateAvailable = latestVersion.CompareTo(currentVersion) > 0;

        var featureText = GetFeatureText(manifest, uiCulture);

        return isUpdateAvailable
            ? [normalizedVersion, featureText]
            : [currentVersion.ToString()];
    }


    /// <summary>
    /// Returns version featured changes text for the specified UI culture.
    /// </summary>
    private static string GetFeatureText(UpdateManifest manifest, string? uiCulture)
    {
        if (manifest.Features == null || manifest.Features.Count == 0)
        {
            return string.Empty;
        }

        var map = new Dictionary<string, string>(manifest.Features, StringComparer.OrdinalIgnoreCase);

        static string? TryGet(Dictionary<string, string> map, string key)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                return null;
            }

            if (!map.TryGetValue(key, out var value))
            {
                return null;
            }

            return string.IsNullOrWhiteSpace(value) ? null : value;
        }

        if (!string.IsNullOrWhiteSpace(uiCulture))
        {
            var culture = uiCulture.Trim();

            var exact = TryGet(map, culture);
            if (exact != null)
            {
                return exact;
            }
        }

        // Fallback to English
        var english = TryGet(map, "en-US");
        if (english != null)
        {
            return english;
        }

        // As a last resort, returns first non-empty value
        foreach (var value in map.Values)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value;
            }
        }

        return string.Empty;
    }



    /// <summary>
    /// Accepts "X.X.X" or "X.X.X.X" and normalizes to a 4-part version string when needed.
    /// </summary>
    private static string NormalizeVersion(string v)
    {
        var parts = v.Split('.');
        if (parts.Length >= 4)
        {
            return v;
        }

        var sb = new StringBuilder(v);
        for (int i = parts.Length; i < 4; i++)
        {
            sb.Append(".0");
        }

        return sb.ToString();
    }
}