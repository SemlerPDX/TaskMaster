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

namespace TaskMaster.Model;

/// <summary>
/// Indicates that a property is a setting to be saved/loaded.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class SettingAttribute : Attribute
{
    /// <summary>
    /// An optional custom name for the setting; if <see langword="null"/>, the property name is used.
    /// </summary>
    public string? Name { get; }

    /// <summary>
    /// If true, this setting will be ignored during save/load operations.
    /// </summary>
    public bool Ignore { get; init; }


    /// <summary>
    /// Construct a new SettingAttribute.
    /// </summary>
    /// <param name="name">The optional custom name for the setting; if <see langword="null"/>, the property name is used.</param>
    public SettingAttribute(string? name = null)
    {
        Name = name;
    }
}
