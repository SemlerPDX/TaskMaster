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
using System.Runtime.CompilerServices;

namespace TaskMaster.ViewModel;

/// <summary>
/// Base class for ViewModels, implementing INotifyPropertyChanged.
/// </summary>
public abstract class BaseViewModel : INotifyPropertyChanged
{
    /// <summary>
    /// Event raised when a property value changes.
    /// </summary>
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// Raises the PropertyChanged event for the specified property.
    /// </summary>
    /// <param name="propertyName">The name of the property that changed.
    /// Pass null/empty to indicate all properties changed.</param>
    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        var handler = PropertyChanged;
        handler?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    /// <summary>
    /// Raises the PropertyChanged event for multiple properties.
    /// </summary>
    /// <param name="propertyNames">Array of property names that changed.</param>
    protected void OnPropertiesChanged(params string[] propertyNames)
    {
        foreach (var name in propertyNames)
        {
            OnPropertyChanged(name);
        }
    }

    /// <summary>
    /// Sets a property field and raises PropertyChanged if the value changed.
    /// </summary>
    /// <typeparam name="T">The type of the property.</typeparam>
    /// <param name="field">Reference to the backing field.</param>
    /// <param name="value">Value to set.</param>
    /// <param name="name">Name of the property (automatically provided).</param>
    /// <returns>True if the value changed, false if it was the same.</returns>
    protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? name = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return false;
        }

        field = value;
        OnPropertyChanged(name);
        return true;
    }

    /// <summary>
    /// Sets a property field, performs an action, and raises PropertyChanged (plus any dependents) if the value changed.
    /// </summary>
    /// <typeparam name="T">The type of the property.</typeparam>
    /// <param name="field">Reference to the backing field.</param>
    /// <param name="value">Value to set.</param>
    /// <param name="onChanged">Action to perform after the value changes.</param>
    /// <param name="name">Name of the property (automatically provided).</param>
    /// <param name="alsoNotify">Array of property names to raises PropertyChanged for.</param>
    /// <returns>True if the value changed, false if it was the same.</returns>
    protected bool SetProperty<T>(
        ref T field,
        T value,
        Action? onChanged = null,
        [CallerMemberName] string? name = null,
        params string[] alsoNotify)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return false;
        }

        field = value;
        onChanged?.Invoke();

        OnPropertyChanged(name);
        if (alsoNotify is { Length: > 0 })
        {
            OnPropertiesChanged(alsoNotify);
        }

        return true;
    }
}
