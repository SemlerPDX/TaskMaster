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
/// A UI style option with a label and corner radius factor.
/// </summary>
/// <param name="Label">The display label.</param>
/// <param name="Factor">The numeric factor.</param>
public sealed record StyleOption(string Label, int Factor);

/// <summary>
/// Catalog of available UI styles.<br/>
/// <br/>
/// Style Options numeric factor is multipled by the default base factor
/// (see <see cref="StyleService.DefaultCornerRadiusBase"/>) for<br/>
/// final global corner radius value in <see cref="StyleService.CurrentStyle"/>.
/// </summary>
internal static class StyleCatalog
{
    /// <summary>
    /// Available UI style options for application wide corner radius.<br/><br/>
    /// Choices are mapped to numeric factors:<br/>
    /// "Edge" = 1,<br/>
    /// "Subtle" = 2,<br/>
    /// "Rounded" = 4,<br/>
    /// "Pill" = 8
    /// </summary>
    public static IReadOnlyList<StyleOption> StyleOptions { get; } =
    [
        new StyleOption("Edge",    1),
        new StyleOption("Subtle",  2),
        new StyleOption("Rounded", 4),
        new StyleOption("Pill",    8),
    ];
}
