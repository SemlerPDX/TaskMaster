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
using System.Net.Http;

namespace TaskMaster.Services;

/// <summary>
/// Provides methods to retrieve the application's license terms and disclaimer text content.
/// </summary>
internal static class LicenseService
{
    private const string RootUri = "https://raw.githubusercontent.com/SemlerPDX/TaskMaster/refs/heads/main/";

    private const string LicenseFileName = "LICENSE.txt";
    private const string DisclaimerFileName = "DISCLAIMER.txt";

    /// <summary>
    /// Get the full license text, first attempting to read from a local file, then falling back to downloading it.
    /// </summary>
    /// <returns>The full license text content, or empty string if not found.</returns>
    public static async Task<string> GetLicenseTextAsync() => await GetTextContentAsync(LicenseFileName) ?? string.Empty;

    /// <summary>
    /// Get application disclaimer content, first attempting to read from a local file, then falling back to downloading it.
    /// </summary>
    /// <returns>The disclaimer text content, or empty string if not found.</returns>
    public static async Task<string> GetDisclaimerTextAsync() => await GetTextContentAsync(DisclaimerFileName) ?? string.Empty;


    private static async Task<string> GetTextContentAsync(string fileName)
    {
        string local = GetTextFromAppFolder(fileName);

        if (!string.IsNullOrEmpty(local))
        {
            return local;
        }

        string remote = await TryDownloadTextFileAsync(fileName);

        return remote;
    }

    private static string GetTextFromAppFolder(string fileName)
    {
        string baseDir = AppContext.BaseDirectory;
        string path = Path.Combine(baseDir, fileName);

        if (!File.Exists(path))
        {
            return string.Empty;
        }

        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        using var reader = new StreamReader(stream);

        return reader.ReadToEnd();
    }

    private static async Task<string> TryDownloadTextFileAsync(string fileName)
    {
        using var client = new HttpClient();
        client.Timeout = TimeSpan.FromSeconds(10);

        return await client.GetStringAsync(RootUri + fileName);
    }
}
