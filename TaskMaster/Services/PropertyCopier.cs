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

using System.Reflection;

namespace TaskMaster.Services;

/// <summary>
/// Utility to copy public read/write properties from one instance to another of the same type.
/// </summary>
/// <typeparam name="T">The type of the instances to copy between.</typeparam>
internal static class PropertyCopier<T>
{
    private static readonly PropertyInfo[] _props =
        [.. typeof(T).GetProperties(BindingFlags.Instance | BindingFlags.Public).Where(p => p.CanRead && p.CanWrite)];

    /// <summary>
    /// Copy all public read/write properties from one instance to another.
    /// </summary>
    /// <param name="from">The source instance to copy from.</param>
    /// <param name="into">The target instance to copy into.</param>
    public static void Copy(T from, T into)
    {
        foreach (var p in _props)
        {
            var value = p.GetValue(from, null);
            p.SetValue(into, value, null);
        }
    }
}
