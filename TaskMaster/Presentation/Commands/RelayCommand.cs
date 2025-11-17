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

using System.Windows.Input;

namespace TaskMaster.Presentation.Commands;

/// <summary>
/// A command that relays its functionality to other objects by invoking delegates.
/// </summary>
internal sealed class RelayCommand : ICommand
{
    private readonly Action _execute;
    private readonly Func<bool>? _canExecute;


    /// <summary>
    /// Occurs when changes occur that affect whether or not the command should execute.
    /// </summary>
    public event EventHandler? CanExecuteChanged;

    /// <summary>
    /// Initializes a new instance of the <see cref="RelayCommand"/> class that can always execute.
    /// </summary>
    /// <param name="execute"></param>
    public RelayCommand(Action execute) : this(execute, null) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="RelayCommand"/> class.
    /// </summary>
    /// <param name="execute">The command to execute.</param>
    /// <param name="canExecute">A function that determines whether the command can execute.</param>
    public RelayCommand(Action execute, Func<bool>? canExecute)
    {
        ArgumentNullException.ThrowIfNull(execute);
        _execute = execute;
        _canExecute = canExecute;
    }

    /// <summary>
    /// Determines whether the command can execute in its current state.
    /// </summary>
    /// <param name="parameter">The command parameter.</param>
    /// <returns><see langword="True"/> if this command can be executed; otherwise, <see langword="false"/>.</returns>
    public bool CanExecute(object? parameter)
    {
        return _canExecute == null || _canExecute();
    }

    /// <summary>
    /// Executes the command.
    /// </summary>
    /// <param name="parameter">The command parameter.</param>
    public void Execute(object? parameter)
    {
        _execute();
    }

    /// <summary>
    /// Raises the <see cref="CanExecuteChanged"/> event.
    /// </summary>
    public void RaiseCanExecuteChanged()
    {
        CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }
}

/// <summary>
/// A command that relays its functionality to other objects by invoking delegates, with a parameter of type T.
/// </summary>
/// <typeparam name="T">The type of the command parameter.</typeparam>
internal sealed class RelayCommand<T> : ICommand
{
    private readonly Action<T?> _execute;
    private readonly Func<T?, bool>? _canExecute;


    /// <summary>
    /// Occurs when changes occur that affect whether or not the command should execute.
    /// </summary>
    public event EventHandler? CanExecuteChanged;

    /// <summary>
    /// Initializes a new instance of the <see cref="RelayCommand{T}"/> class that can always execute.
    /// </summary>
    /// <param name="execute">The command to execute.</param>
    public RelayCommand(Action<T?> execute) : this(execute, null) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="RelayCommand{T}"/> class.
    /// </summary>
    /// <param name="execute">The command to execute.</param>
    /// <param name="canExecute">A function that determines whether the command can execute.</param>
    public RelayCommand(Action<T?> execute, Func<T?, bool>? canExecute)
    {
        ArgumentNullException.ThrowIfNull(execute);
        _execute = execute;
        _canExecute = canExecute;
    }

    /// <summary>
    /// Determines whether the command can execute in its current state.
    /// </summary>
    /// <param name="parameter">The command parameter.</param>
    /// <returns><see langword="True"/> if this command can be executed; otherwise, <see langword="false"/>.</returns>
    public bool CanExecute(object? parameter)
    {
        return _canExecute == null || _canExecute((T?)parameter);
    }

    /// <summary>
    /// Executes the command.
    /// </summary>
    /// <param name="parameter">The command parameter.</param>
    public void Execute(object? parameter)
    {
        _execute((T?)parameter);
    }

    /// <summary>
    /// Raises the <see cref="CanExecuteChanged"/> event.
    /// </summary>
    public void RaiseCanExecuteChanged()
    {
        CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }
}
