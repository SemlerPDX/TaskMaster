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

using System.Windows.Data;

namespace TaskMaster.Converters;

/// <summary>
/// Converts a string with line breaks into an array of lines.
/// </summary>
public sealed class StringToLinesConverter : IValueConverter
{
    /// <summary>
    /// Converts a string with line breaks into an array of lines.
    /// </summary>
    /// <param name="value">The input string.</param>
    /// <param name="targetType">The target type.</param>
    /// <param name="parameter">An optional parameter.</param>
    /// <param name="culture">The culture info.</param>
    /// <returns></returns>
    public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
        string text = value as string ?? string.Empty;
        return text.Split(["\r\n", "\n"], StringSplitOptions.None);
    }

    /// <summary>
    /// Not implemented.
    /// </summary>
    /// <param name="value">The input string.</param>
    /// <param name="targetType">The target type.</param>
    /// <param name="parameter">An optional parameter.</param>
    /// <param name="culture">The culture info.</param>
    /// <returns></returns>
    public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
        return System.Windows.Data.Binding.DoNothing;
    }
}
