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
using System.Security.Cryptography;
using System.Text;
using System.Xml;
using System.Xml.Serialization;

using TaskMaster.Application;
using TaskMaster.Services.Logging;

namespace TaskMaster.Services;

/// <summary>
/// A service to manage loading and saving application configuration data.<br/>
/// This implementation uses DPAPI to protect the config file in a "best effort" for the current user.
/// </summary>
public sealed class ConfigFileHandler : IConfigFileHandler
{
    private static readonly string ConfigName = "config";
    private static readonly string ConfigExt = ".dat";
    private static readonly string DebugExt = ".debug.xml";

    /// <summary>
    /// Get the full path to the config file in the user's AppData directory.
    /// </summary>
    /// <param name="fileName">The config file name without extension (optional, defaults to "config").</param>
    /// <param name="folderName">The folder name under AppData (optional, defaults to application name).</param>
    /// <returns>The full path to the application config file.</returns>
    public string GetConfigPath(string? fileName = null, string? folderName = null)
    {
        var configFileName = (fileName ?? ConfigName) + ConfigExt;
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var appName = AppInfo.Name;

        var dir = Path.Combine(appData, folderName ?? appName);
        Directory.CreateDirectory(dir);

        return Path.Combine(dir, configFileName);
    }

    /// <summary>
    /// Delete all config files (main and debug) and the config directory if empty.
    /// </summary>
    /// <returns><see langword="True"/> if successful, <see langword="false"/> if otherwise.</returns>
    public bool DeleteAllConfigs()
    {
        try
        {
            var path = GetConfigPath();

            var dir = Path.GetDirectoryName(path);
            if (dir != null && Directory.Exists(dir))
            {
                DeleteFolderRecursive(dir);
            }

            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Save the given config object to disk, protected with DPAPI for the current user ("best effort").
    /// </summary>
    /// <typeparam name="T">The type of the config object (must be XML serializable).</typeparam>
    /// <param name="config">The config object to save.</param>
    public void SaveConfig<T>(T config)
    {
        var path = GetConfigPath();
        byte[] raw = [];
        try
        {
            var xs = new XmlSerializer(typeof(T));
            using var ms = new MemoryStream();
            using (var xw = XmlWriter.Create(ms, new XmlWriterSettings { Indent = true, Encoding = Encoding.UTF8 }))
            {
                xs.Serialize(xw, config);
            }

            raw = ms.ToArray();
        }
        catch (FileNotFoundException ex)
        {
            Log.Error(ex, $"Config file path not found at path: {path}");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Attempted and Failed to serialize config object to XML.");
            return;
        }

        if (raw.Length == 0)
        {
            Log.Warn("Serialized config object is null or empty.");
            return;
        }

        // Protect with DPAPI for the current user
        byte[] protectedData = ProtectedData.Protect(raw, null, DataProtectionScope.CurrentUser);

        DebugWriteToUnprotectedXmlFile(path, Encoding.UTF8.GetString(raw));

        WriteAtomic(path, protectedData);

        // Lock down file permissions to current user only (best effort)
        TryRestrictFileToCurrentUser(path);
    }

    /// <summary>
    /// Load the config object from disk, returning null if not found or if unprotect fails.
    /// </summary>
    /// <typeparam name="T">The type of the config object (must be XML serializable).</typeparam>
    /// <returns>The loaded config object, or null if not found or if unprotect fails.</returns>
    public T? LoadConfig<T>() where T : class
    {
        var path = GetConfigPath();
        if (!File.Exists(path))
        {
            return null;
        }

        var protectedData = File.ReadAllBytes(path);
        byte[] raw;
        try
        {
            raw = ProtectedData.Unprotect(protectedData, null, DataProtectionScope.CurrentUser);
        }
        catch (CryptographicException)
        {
            // failed to unprotect => tampered or wrong user
            Log.Debug("Failed to unprotect config file data - possibly tampered or wrong user.");
            return null;
        }

        // safely deserialize with XmlReader (no DTD)
        var settings = new XmlReaderSettings
        {
            DtdProcessing = DtdProcessing.Prohibit,
            XmlResolver = null
        };

        try
        {
            var xs = new XmlSerializer(typeof(T));

            using var ms = new MemoryStream(raw);
            using var xr = XmlReader.Create(ms, settings);
            var obj = xs.Deserialize(xr);

            return obj as T;
        }
        catch (FileNotFoundException ex)
        {
            Log.Error(ex, $"Config file path not found at path: {path}");
            return null;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Attempted and Failed to deserialize config object from XML.");
            return null;
        }
    }

    private static void TryRestrictFileToCurrentUser(string path)
    {
        try
        {
            // Assert ACL policy
            var fileInfo = new FileInfo(path);
            var accessLayer = fileInfo.GetAccessControl();

            // Remove existing rules for save file... only grant full control to current user
            var user = System.Security.Principal.WindowsIdentity.GetCurrent().User;
            if (user == null)
            {
                return; // cannot determine user... at least we tried...
            }

            var rights = System.Security.AccessControl.FileSystemRights.FullControl;
            var rule = new System.Security.AccessControl.FileSystemAccessRule(user,
                rights,
                System.Security.AccessControl.InheritanceFlags.None,
                System.Security.AccessControl.PropagationFlags.NoPropagateInherit,
                System.Security.AccessControl.AccessControlType.Allow);

            // Remove access rules then add save file protection (best effort)
            accessLayer.SetAccessRuleProtection(isProtected: true, preserveInheritance: false);
            accessLayer.ResetAccessRule(rule);
            fileInfo.SetAccessControl(accessLayer);
        }
        catch
        {
            // best effort... let it slide on failure (some systems might deny ACL changes)
            Log.Debug($"Failed to restrict config file access to current user only at path '{path}'");
        }
    }

    private static void WriteAtomic(string path, byte[] protectedData)
    {
        string dir = Path.GetDirectoryName(path)!;
        Directory.CreateDirectory(dir);

        string tmp = path + ".tmp";
        string bak = path + ".bak";

        using (var fs = new FileStream(
            tmp,
            FileMode.Create,
            FileAccess.Write,
            FileShare.None,
            4096,
            FileOptions.WriteThrough)) // more durable writes
        {
            fs.Write(protectedData, 0, protectedData.Length);
            fs.Flush(true); // flush file + OS buffers
        }

        // Atomically swap
        if (File.Exists(path))
        {
            File.Replace(tmp, path, bak, ignoreMetadataErrors: true);
        }
        else
        {
            // First write: no existing file to replace (atomic rename)
            File.Move(tmp, path);
        }
    }

    private static void DeleteFolderRecursive(string dir)
    {
        if (!Directory.Exists(dir))
        {
            return;
        }

        // Guard for folder name - must equal appname
        var folderName = Path.GetFileName(dir);
        if (!string.Equals(folderName, AppInfo.Name, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var files = Directory.GetFiles(dir);
        foreach (var file in files)
        {
            File.Delete(file);
        }

        var subdirs = Directory.GetDirectories(dir);
        foreach (var subdir in subdirs)
        {
            DeleteFolderRecursive(subdir);
        }

        Directory.Delete(dir);
    }

    private static void DebugWriteToUnprotectedXmlFile(string filePath, string xmlContent)
    {
        // For debugging purposes only: write the unprotected XML to a separate file
        try
        {
            if (Log.Current.MinimumLevel > LogLevel.Debug)
            {
                return;
            }

            var debugPath = filePath + DebugExt;
            File.WriteAllText(debugPath, xmlContent);
        }
        catch
        {
            // ignore
        }
    }
}
