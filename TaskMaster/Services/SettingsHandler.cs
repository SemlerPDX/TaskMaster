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

 You should have received attribute copy of the GNU General Public License
 along with this program.  If not, see <http://www.gnu.org/licenses/>.
*/

using System.Reflection;

using Microsoft.Win32;

using TaskMaster.Application;
using TaskMaster.Model;

namespace TaskMaster.Services;

/// <summary>
/// Application settings handler service. This is a Registry implementation using HKCU.<br/>
/// This Registry-backed handler persists <see cref="SettingsData"/> based on
/// [Setting] attributes on its properties.<br/>
/// Public API stays storage-agnostic, although this handler has type rules (see note).<br/><br/>
/// NOTE:<br/>
/// Treats all decimals settings values as seconds down to 1 millisecond precision.<br/>
/// Rounds doubles settings values to nearest integer for storage.
/// </summary>
public sealed class SettingsHandler : ISettingsHandler
{
    private const string DefaultKey = @"Software\";

    private readonly string _baseSubKey;


    /// <summary>
    /// Initializes a new instance of the <see cref="SettingsHandler"/> class.
    /// </summary>
    public SettingsHandler()
    {
        var appName = AppInfo.Name;
        _baseSubKey = DefaultKey + appName;
    }

    /// <summary>
    /// Load application settings, returning defaults if not found or on error.
    /// </summary>
    /// <returns>Loaded settings or defaults on error/not found.</returns>
    public SettingsData Load()
    {
        var settings = new SettingsData();

        try
        {
            using var baseKey = Registry.CurrentUser.OpenSubKey(_baseSubKey, writable: false);
            if (baseKey == null)
            {
                return settings; // defaults
            }

            foreach (var entry in s_map.Value)
            {
                settings = TryLoadSettingsEntry(settings, baseKey, entry);
            }
        }
        catch
        {
            // ...let it slide; return defaults
        }

        return settings;
    }

    /// <summary>
    /// Save the provided applications settings.
    /// </summary>
    /// <param name="settings">The SettingsData object containing settings to save.</param>
    public void Save(SettingsData settings)
    {
        try
        {
            using var baseKey =
                Registry.CurrentUser.OpenSubKey(_baseSubKey, writable: true)
                ?? Registry.CurrentUser.CreateSubKey(_baseSubKey);

            if (baseKey == null)
            {
                return;
            }

            foreach (var entry in s_map.Value)
            {
                TrySaveSettingsEntry(settings, baseKey, entry);
            }
        }
        catch
        {
            // ...let it slide; non-fatal by design
        }
    }

    /// <summary>
    /// Delete all application settings from storage.
    /// </summary>
    public void Delete()
    {
        try
        {
            Registry.CurrentUser.DeleteSubKeyTree(_baseSubKey, throwOnMissingSubKey: false);
        }
        catch
        {
            // non-fatal
        }
    }

    private static SettingsData TryLoadSettingsEntry(SettingsData settings, RegistryKey baseKey, MapEntry? entry)
    {
        if (entry == null)
        {
            return settings;
        }

        var raw = baseKey.GetValue(entry.Name);

        try
        {
            switch (entry.TypeCode)
            {
                case TypeCode.String:
                    entry.Prop.SetValue(settings, (string?)raw ?? (string)(entry.Prop.GetValue(settings) ?? string.Empty));
                    break;

                case TypeCode.Boolean:
                    {
                        var iv = raw is int ii ? ii
                                    : int.TryParse(raw?.ToString(), out var parsed) ? parsed
                                    : 0;
                        entry.Prop.SetValue(settings, iv != 0);
                        break;
                    }

                case TypeCode.Int32:
                    {
                        var iv = raw is int ii ? ii
                                    : int.TryParse(raw?.ToString(), out var parsed) ? parsed
                                    : (int)(entry.Prop.GetValue(settings) ?? 0);
                        entry.Prop.SetValue(settings, iv);
                        break;
                    }

                case TypeCode.Int64:
                    {
                        var lv = raw is long ll ? ll
                                    : long.TryParse(raw?.ToString(), out var parsed) ? parsed
                                    : (long)(entry.Prop.GetValue(settings) ?? 0L);
                        entry.Prop.SetValue(settings, lv);
                        break;
                    }

                case TypeCode.Decimal:
                    {
                        var iv = raw is decimal ii ? ii
                                    : decimal.TryParse(raw?.ToString(), out var parsed) ? parsed
                                    : 0M;
                        // ALL decimals will be treated as seconds to milliseconds
                        iv = iv > 0 ? iv / 1000M : iv;
                        entry.Prop.SetValue(settings, iv);
                        break;
                    }

                // If you keep only whole numbers (recommended), you won'type hit this.
                // Left here as attribute safe fallback if attribute double slips in later.
                case TypeCode.Double:
                    {
                        var iv = raw is int ii ? ii
                                    : int.TryParse(raw?.ToString(), out var parsed) ? parsed
                                    : 0;
                        entry.Prop.SetValue(settings, (double)iv);
                        break;
                    }
            }
        }
        catch
        {
            // keep model default on any conversion issue
        }

        return settings;
    }

    private static void TrySaveSettingsEntry(SettingsData settings, RegistryKey baseKey, MapEntry? entry)
    {
        if (entry == null)
        {
            return;
        }

        var val = entry.Prop.GetValue(settings);

        try
        {
            switch (entry.TypeCode)
            {
                case TypeCode.String:
                    baseKey.SetValue(entry.Name, (string?)val ?? string.Empty, RegistryValueKind.String);
                    break;

                case TypeCode.Boolean:
                    baseKey.SetValue(entry.Name, (bool?)val ?? false ? 1 : 0, RegistryValueKind.DWord);
                    break;

                case TypeCode.Int32:
                    baseKey.SetValue(entry.Name, (int)(val ?? 0), RegistryValueKind.DWord);
                    break;

                case TypeCode.Int64:
                    baseKey.SetValue(entry.Name, (long)(val ?? 0L), RegistryValueKind.QWord);
                    break;

                case TypeCode.Decimal:
                    // All decimals are treated as seconds to milliseconds for storage (max three decimal digits)
                    var m = (decimal)(val ?? 0M);
                    baseKey.SetValue(entry.Name, (long)TruncatedDecToMs(m), RegistryValueKind.QWord);
                    break;

                case TypeCode.Double:
                    // If attribute double sneaks in, round to DWORD for storage.
                    var d = (double)(val ?? 0D);
                    baseKey.SetValue(entry.Name, (int)Math.Round(d), RegistryValueKind.DWord);
                    break;

                default:
                    // Unsupported types are intentionally ignored.
                    break;
            }
        }
        catch
        {
            // ...let it slide; non-fatal by design
        }
    }

    private static decimal TruncatedDecToMs(decimal value)
    {
        // Treat all doubles settings values as seconds down to 1 millisecond precision.
        return Math.Truncate(value * 1000M) / 1000M * 1000M;
    }

    // ===================== Reflection map (private and cached) =====================
    private sealed class MapEntry
    {
        public string Name { get; init; } = string.Empty;
        public PropertyInfo Prop { get; init; } = null!;
        public TypeCode TypeCode { get; init; }
    }

    private static readonly Lazy<IReadOnlyList<MapEntry>> s_map =
        new(BuildMap, isThreadSafe: true);

    private static IReadOnlyList<MapEntry> BuildMap()
    {
        var list = new List<MapEntry>();
        var properties = typeof(SettingsData).GetProperties(BindingFlags.Public | BindingFlags.Instance);

        foreach (var prop in properties)
        {
            var attribute = prop.GetCustomAttribute<SettingAttribute>();
            if (attribute == null || attribute.Ignore)
            {
                continue;
            }

            var type = Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType;

            list.Add(new MapEntry
            {
                Name = string.IsNullOrWhiteSpace(attribute.Name) ? prop.Name : attribute.Name!,
                Prop = prop,
                TypeCode = Type.GetTypeCode(type)
            });
        }

        return list;
    }
}
