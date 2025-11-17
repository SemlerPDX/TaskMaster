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

using TaskMaster.Services.Logging;

namespace TaskMaster.Presentation.UI;

/// <summary>
/// Helper class to update design tokens at runtime.
/// </summary>
internal static class DesignTokenUpdater
{
    private const string DesignTokensDictionary = "DesignTokens.xaml";
    private const string TimeToolTipInitialDelayKey = "Time.ToolTip.InitialDelay";
    private const string TimeToolTipDurationKey = "Time.ToolTip.Duration";
    private const string CornerRadiusStyleKey = "CornerRadius.CurrentStyle";

    private const int DefaultInitialDelay = 500; // ms
    private const int DefaultDuration = 15000;    // ms

    /// <summary>
    /// Sets the tooltip timing design tokens at runtime.
    /// </summary>
    /// <param name="initialDelayMs">The initial delay in milliseconds.</param>
    /// <param name="durationMs">The duration in milliseconds.</param>
    public static void SetToolTipTimings(int? initialDelayMs, int? durationMs)
    {
        try
        {
            var app = System.Windows.Application.Current;
            if (app == null)
            {
                return;
            }

            initialDelayMs ??= DefaultInitialDelay;
            durationMs ??= DefaultDuration;

            // Try to update the actual DesignTokens dictionary if present;
            // fall back to the root Application resources otherwise.
            ResourceDictionary target = FindDesignTokensDictionary(app.Resources) ?? app.Resources;

            // Replace the boxed Int32 values. DynamicResource listeners will update.
            target[TimeToolTipInitialDelayKey] = initialDelayMs;
            target[TimeToolTipDurationKey] = durationMs;
        }
        catch (Exception ex)
        {
            // Ignore and log failures
            Log.Error(ex, "Failed to update tooltip timing design tokens.");
        }
    }

    /// <summary>
    /// Sets the corner radius style design token at runtime.
    /// </summary>
    /// <param name="radius">The corner radius in pixels.</param>
    public static void SetCornerRadiusStyle(int radius)
    {
        try
        {
            var app = System.Windows.Application.Current;
            if (app == null)
            {
                return;
            }

            ResourceDictionary target = FindDesignTokensDictionary(app.Resources) ?? app.Resources;
            target[CornerRadiusStyleKey] = new CornerRadius(radius);
        }
        catch (Exception ex)
        {
            // Ignore and log failures
            Log.Error(ex, "Failed to update corner radius design token.");
        }
    }

    private static ResourceDictionary? FindDesignTokensDictionary(ResourceDictionary root)
    {
        foreach (var dict in root.MergedDictionaries)
        {
            var src = dict.Source?.OriginalString ?? string.Empty;
            if (src.EndsWith(DesignTokensDictionary, StringComparison.OrdinalIgnoreCase))
            {
                return dict;
            }
        }

        return null;
    }
}
