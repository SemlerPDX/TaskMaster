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

using System.Diagnostics;

using TaskMaster.Application.Composition;
using TaskMaster.Services.Logging;

namespace TaskMaster.Services;

/// <summary>
/// Service for handling URI operations.
/// </summary>
public static class UriService
{
    private const string OpenUriFailMessageKey = "Message.App.History.LinkFailed";


    /// <summary>
    /// Try to open a link in the default system browser.
    /// </summary>
    /// <param name="features">The feature services.</param>
    /// <param name="url">The URL to open.</param>
    public static void OpenLink(IFeatureServices features, string? url)
    {
        try
        {
            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            {
                throw new Exception("Failed to create Uri from string.");
            }

            Process.Start(new ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            Log.Error(ex, $"Failed to open link from Help dialog. {url}");

            InfoService.LogLauncherHistory(features, OpenUriFailMessageKey);
            InfoService.LogKillerHistory(features, OpenUriFailMessageKey);
        }
    }
}
