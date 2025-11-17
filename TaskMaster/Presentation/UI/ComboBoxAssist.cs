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
using System.Windows.Controls.Primitives;
using System.Windows.Media;

namespace TaskMaster.Presentation.UI;

/// <summary>
/// Template parameterization helpers for ComboBox.<br/>
/// Allows setting the PopupPlacement (top or bottom) of the ComboBox's dropdown popup via an attached property.<br/>
/// Also allows setting a custom IconGeometry for the ComboBox's toggle button via an attached property.
/// </summary>
public static class ComboBoxAssist
{
    /// <summary>
    /// Identifies the PopupPlacement attached property.
    /// </summary>
    public static readonly DependencyProperty PopupPlacementProperty =
        DependencyProperty.RegisterAttached(
            "PopupPlacement",
            typeof(PlacementMode),
            typeof(ComboBoxAssist),
            new FrameworkPropertyMetadata(PlacementMode.Bottom, FrameworkPropertyMetadataOptions.Inherits));

    /// <summary>
    /// Sets the PopupPlacement attached property.
    /// </summary>
    /// <param name="element">The element on which to set the property.</param>
    /// <param name="value">The placement mode value (top or bottom).</param>
    public static void SetPopupPlacement(DependencyObject element, PlacementMode value)
    {
        element.SetValue(PopupPlacementProperty, value);
    }

    /// <summary>
    /// Gets the PopupPlacement attached property.
    /// </summary>
    /// <param name="element">The element from which to get the property.</param>
    /// <returns>The placement mode value (top or bottom).</returns>
    public static PlacementMode GetPopupPlacement(DependencyObject element)
    {
        return (PlacementMode)element.GetValue(PopupPlacementProperty);
    }

    /// <summary>
    /// Identifies the IconGeometry attached property.
    /// </summary>
    public static readonly DependencyProperty IconGeometryProperty =
        DependencyProperty.RegisterAttached(
            "IconGeometry",
            typeof(Geometry),
            typeof(ComboBoxAssist),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.Inherits));

    /// <summary>
    /// Sets the IconGeometry attached property.
    /// </summary>
    /// <param name="element">The element on which to set the property.</param>
    /// <param name="value">The Geometry value.</param>
    public static void SetIconGeometry(DependencyObject element, Geometry value)
    {
        element.SetValue(IconGeometryProperty, value);
    }

    /// <summary>
    /// Gets the IconGeometry attached property.
    /// </summary>
    /// <param name="element">The element from which to get the property.</param>
    /// <returns>The Geometry value.</returns>
    public static Geometry GetIconGeometry(DependencyObject element)
    {
        return (Geometry)element.GetValue(IconGeometryProperty);
    }
}