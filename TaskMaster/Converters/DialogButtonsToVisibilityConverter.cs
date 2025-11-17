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

using System.Globalization;
using System.Windows;
using System.Windows.Data;

using TaskMaster.ViewModel.Dialog;

namespace TaskMaster.Converters;

/// <summary>
/// Converter for DialogButtons enum values to Visibility based on the specified button part.<br/>
/// Maps DialogButtons (Ok, OkCancel, YesNo, YesNoCancel) + parameter ("Ok"|"Cancel"|"Yes"|"No") to Visible/Collapsed
/// </summary>
public sealed class DialogButtonsToVisibilityConverter : IValueConverter
{
    /// <summary>
    /// Converts DialogButtons enum value and parameter to Visibility.=
    /// </summary>
    /// <param name="value">The DialogButtons enum value.</param>
    /// <param name="targetType">The target type (should be Visibility).</param>
    /// <param name="parameter">Parameter of "Ok", "Cancel", "Yes", or "No" to indicate which button's visibility to determine.</param>
    /// <param name="culture">The currently active culture info.</param>
    /// <returns>The Visibility value for the specified button part.</returns>
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (parameter is null || value is null)
        {
            return Visibility.Collapsed;
        }

        if (value is not DialogButtons buttons)
        {
            return Visibility.Collapsed;
        }

        string part = parameter.ToString() ?? string.Empty;

        switch (part)
        {
            case "Ok":
                {
                    // Show OK when the set is Ok or OkCancel
                    bool show = buttons == DialogButtons.Ok || buttons == DialogButtons.OkCancel;
                    return show ? Visibility.Visible : Visibility.Collapsed;
                }
            case "Cancel":
                {
                    // Show Cancel when the set is OkCancel or YesNoCancel
                    bool show = buttons == DialogButtons.OkCancel || buttons == DialogButtons.YesNoCancel;
                    return show ? Visibility.Visible : Visibility.Collapsed;
                }
            case "Yes":
                {
                    // Show Yes when the set is YesNo or YesNoCancel
                    bool show = buttons == DialogButtons.YesNo || buttons == DialogButtons.YesNoCancel;
                    return show ? Visibility.Visible : Visibility.Collapsed;
                }
            case "No":
                {
                    // Show No when the set is YesNo or YesNoCancel
                    bool show = buttons == DialogButtons.YesNo || buttons == DialogButtons.YesNoCancel;
                    return show ? Visibility.Visible : Visibility.Collapsed;
                }
            default:
                {
                    return Visibility.Collapsed;
                }
        }
    }

    /// <summary>
    /// Not supported: ConvertBack is not implemented.
    /// </summary>
    /// <returns>Not supported.</returns>
    /// <exception cref="NotSupportedException">Will always be thrown.</exception>
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
