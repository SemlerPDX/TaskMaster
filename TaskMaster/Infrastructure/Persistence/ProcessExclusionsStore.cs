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
using System.Xml.Serialization;

using TaskMaster.Application;
using TaskMaster.Services.Logging;

namespace TaskMaster.Infrastructure.Persistence;

/// <summary>
/// XML-serializable configuration for process exclusions.
/// </summary>
[XmlRoot("ProcessExclusions")]
public sealed class ProcessExclusionsConfig
{
    /// <summary>
    /// The list of excluded process names (bare, no “.exe”).
    /// </summary>
    [XmlArray("Names")]
    [XmlArrayItem("Name")]
    public List<string> Names { get; set; } = [];
}

/// <summary>
/// A service to manage loading process exclusions from persistent storage.
/// </summary>
public sealed class ProcessExclusionsStore : IProcessExclusionsStore
{
    private const string ExclusionsFolderName = "config";
    private const string ExclusionsFileName = "ProcessExclusions.xml";

    private readonly HashSet<string> _names = new(StringComparer.OrdinalIgnoreCase);
    private string _filePath = string.Empty;

    private readonly string[] RequiredExclusions = [
        "TaskMaster",
        "Idle",
        "svchost",
        "sihost",
        "services",
        "rundll32",
        "Registry",
        "System"
    ];


    /// <summary>
    /// Event raised when the exclusions have changed.<br/>
    /// (present but unused in current version)
    /// </summary>
    public event Action? Changed;

    /// <summary>
    /// Loads the process exclusions from persistent storage.
    /// </summary>
    /// <returns>A read-only collection of excluded process names.</returns>
    public IReadOnlyCollection<string> Load()
    {
        var appDir = Path.GetDirectoryName(AppInfo.Location) ?? AppDomain.CurrentDomain.BaseDirectory;
        _filePath = Path.Combine(appDir, ExclusionsFolderName, ExclusionsFileName);

        LoadFromXmlIfPresent(_filePath);
        LoadRequiredExclusions();

        return _names ?? new(StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Reloads the exclusions from persistent storage.<br/>
    /// (present but unused in current version)
    /// </summary>
    public void Reload()
    {
        LoadFromXmlIfPresent(_filePath);
        LoadRequiredExclusions();
        Changed?.Invoke();
    }


    private void LoadRequiredExclusions()
    {
        foreach (var exclusion in RequiredExclusions)
        {
            AddSanitized(exclusion);
        }
    }

    private void LoadFromXmlIfPresent(string path)
    {
        try
        {
            if (!File.Exists(path))
            {
                return;
            }

            using var stream = File.OpenRead(path);
            var xml = new XmlSerializer(typeof(ProcessExclusionsConfig));
            if (xml.Deserialize(stream) is ProcessExclusionsConfig cfg && cfg.Names != null)
            {
                foreach (var name in cfg.Names)
                {
                    AddSanitized(name);
                }
            }
        }
        catch
        {
            // Ignore "bad" files... the UI should keep working...
            Log.Warn($"Failed to load process exclusions from XML file at '{path}'");
        }
    }

    private void AddSanitized(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return;
        }

        var bare = Path.GetFileNameWithoutExtension(name.Trim());
        if (string.IsNullOrWhiteSpace(bare))
        {
            return;
        }

        if (_names.Contains(bare))
        {
            return;
        }

        _names.Add(bare);
    }
}
