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

namespace TaskMaster.Presentation.UI;

/// <summary>
/// Service that manages application styles.<br/>
/// <br/>
/// <see cref="StyleCatalog.StyleOptions"/> numeric factor is multipled by the default base factor
/// (see <see cref="DefaultCornerRadiusBase"/>) for<br/>
/// final global corner radius value in <see cref="CurrentStyle"/>.
/// </summary>
public sealed class StyleService : IStyleService
{
    private const int DefaultCornerRadiusBase = 2;

    /// <summary>
    /// Convenience computed value for views that want the real uniform CornerRadius number.
    /// </summary>
    public int CurrentStyle { get; private set; } = DefaultCornerRadiusBase;

    /// <summary>
    /// Event raised when the style is changed.
    /// </summary>
    public event EventHandler? StyleChanged;


    /// <summary>
    /// Applies the specified style by updating the design tokens resource dictionary.<br/>
    /// Corner radius factor is multiplied by base factor for final radius.<br/><br/>
    /// See <see cref="DefaultCornerRadiusBase"/> for base factor value.
    /// </summary>
    /// <param name="factor">The global corner radius factor.</param>
    public void ApplyStyle(int factor)
    {
        int radius = DefaultCornerRadiusBase * factor;   // 1/2/4/8 → 2/4/8/16
        CurrentStyle = radius;

        DesignTokenUpdater.SetCornerRadiusStyle(radius);

        StyleChanged?.Invoke(this, EventArgs.Empty);
    }
}
