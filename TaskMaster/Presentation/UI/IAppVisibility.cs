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
/// Service that tracks application window visibility, minimized state, and tray state.
/// </summary>
public interface IAppVisibility
{
    /// <summary>
    /// Gets whether the application window is currently visible.
    /// </summary>
    bool IsVisible { get; }

    /// <summary>
    /// Gets whether the application window is currently minimized.
    /// </summary>
    bool IsMinimized { get; }

    /// <summary>
    /// Gets whether the application is currently in the system tray.
    /// </summary>
    bool IsInTray { get; }

    /// <summary>
    /// Occurs when the visibility state changes.
    /// </summary>
    event EventHandler? Changed;

    /// <summary>
    /// Attaches the service to the specified window.
    /// </summary>
    /// <param name="window">The window to attach to.</param>
    void Attach(System.Windows.Window window);

    /// <summary>
    /// Detaches the service from the currently attached window.
    /// </summary>
    void Detach();
}
