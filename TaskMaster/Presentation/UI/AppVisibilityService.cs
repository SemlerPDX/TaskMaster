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

using System.ComponentModel;
using System.Windows;

namespace TaskMaster.Presentation.UI;

/// <summary>
/// Service that tracks application window visibility, minimized state, and tray state.
/// </summary>
public sealed class AppVisibilityService : IAppVisibility, IDisposable
{
    private Window? _window;
    private DependencyPropertyDescriptor? _isVisibleDesc;
    private DependencyPropertyDescriptor? _showInTaskbarDesc;

    /// <summary>
    /// Gets whether the application window is currently visible.
    /// </summary>
    public bool IsVisible { get; private set; }

    /// <summary>
    /// Gets whether the application window is currently minimized.
    /// </summary>
    public bool IsMinimized { get; private set; }
    /// <summary>
    /// Gets whether the application is currently in the system tray.
    /// </summary>
    public bool IsInTray { get; private set; }

    /// <summary>
    /// Occurs when the visibility state changes.
    /// </summary>
    public event EventHandler? Changed;

    /// <summary>
    /// Attaches the service to the specified window.
    /// </summary>
    /// <param name="window">The window to attach to.</param>
    public void Attach(Window window)
    {
        ArgumentNullException.ThrowIfNull(window);

        _window = window;

        _window.StateChanged += OnAnyChanged;

        _isVisibleDesc = DependencyPropertyDescriptor.FromProperty(UIElement.IsVisibleProperty, typeof(Window));
        _isVisibleDesc.AddValueChanged(_window, OnAnyChanged);

        _showInTaskbarDesc = DependencyPropertyDescriptor.FromProperty(Window.ShowInTaskbarProperty, typeof(Window));
        _showInTaskbarDesc.AddValueChanged(_window, OnAnyChanged);

        Recompute();
    }

    /// <summary>
    /// Detaches the service from the currently attached window.
    /// </summary>
    public void Detach()
    {
        if (_window == null)
        {
            return;
        }

        _window.StateChanged -= OnAnyChanged;

        if (_isVisibleDesc != null)
        {
            _isVisibleDesc.RemoveValueChanged(_window, OnAnyChanged);
            _isVisibleDesc = null;
        }

        if (_showInTaskbarDesc != null)
        {
            _showInTaskbarDesc.RemoveValueChanged(_window, OnAnyChanged);
            _showInTaskbarDesc = null;
        }

        _window = null;
    }

    private void OnAnyChanged(object? sender, EventArgs e)
    {
        Recompute();
    }

    private void Recompute()
    {
        if (_window == null)
        {
            return;
        }

        bool isVisible = _window.IsVisible;
        bool isMin = _window.WindowState == WindowState.Minimized;
        bool inTray = !_window.ShowInTaskbar;
        bool changed = isVisible != IsVisible || isMin != IsMinimized || inTray != IsInTray;

        IsVisible = isVisible;
        IsMinimized = isMin;
        IsInTray = inTray;

        if (changed)
        {
            Changed?.Invoke(this, EventArgs.Empty);
        }
    }

    /// <summary>
    /// Disposes the service, detaching from the window if necessary.
    /// </summary>
    public void Dispose()
    {
        Detach();
    }
}
