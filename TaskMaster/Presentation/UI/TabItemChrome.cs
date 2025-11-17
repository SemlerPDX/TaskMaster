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

namespace TaskMaster.Presentation.UI;

/// <summary>
/// Template parameterization helpers for TabItemChrome.
/// </summary>
public sealed class TabItemChrome
{
    /// <summary>
    /// Identifies the CornerRadius attached property.
    /// </summary>
    internal static readonly DependencyProperty CornerRadiusProperty =
        DependencyProperty.RegisterAttached(
            "CornerRadius",
            typeof(CornerRadius),
            typeof(TabItemChrome),
            new FrameworkPropertyMetadata(
                new CornerRadius(0),
                FrameworkPropertyMetadataOptions.AffectsRender |
                FrameworkPropertyMetadataOptions.Inherits));

    /// <summary>
    /// Sets the CornerRadius attached property.
    /// </summary>
    /// <param name="obj">The object on which to set the property.</param>
    /// <param name="value">The corner radius value(s).</param>
    internal static void SetCornerRadius(DependencyObject obj, CornerRadius value)
    {
        obj.SetValue(CornerRadiusProperty, value);
    }

    /// <summary>
    /// Gets the CornerRadius attached property.
    /// </summary>
    /// <param name="obj">The object from which to get the property.</param>
    /// <returns>The corner radius value(s).</returns>
    internal static CornerRadius GetCornerRadius(DependencyObject obj)
    {
        return (CornerRadius)obj.GetValue(CornerRadiusProperty);
    }
}
