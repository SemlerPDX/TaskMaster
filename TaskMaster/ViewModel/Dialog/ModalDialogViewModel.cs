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
using System.Windows.Input;

using TaskMaster.Presentation.Commands;

namespace TaskMaster.ViewModel.Dialog;

/// <summary>
/// Possible button configurations for a modal dialog.
/// </summary>
public enum DialogButtons
{
    /// <summary>
    /// Ok button only.
    /// </summary>
    Ok,

    /// <summary>
    /// Ok and Cancel buttons.
    /// </summary>
    OkCancel,

    /// <summary>
    /// Yes and No buttons.
    /// </summary>
    YesNo,

    /// <summary>
    /// Yes, No, and Cancel buttons.
    /// </summary>
    YesNoCancel
}

/// <summary>
/// Possible results from a modal dialog.
/// </summary>
public enum DialogResult
{
    /// <summary>
    /// No result.
    /// </summary>
    None,

    /// <summary>
    /// Ok result.
    /// </summary>
    Ok,

    /// <summary>
    /// Cancel result.
    /// </summary>
    Cancel,

    /// <summary>
    /// Yes result.
    /// </summary>
    Yes,

    /// <summary>
    /// No result.
    /// </summary>
    No
}

/// <summary>
/// View model for a modal dialog.
/// </summary>
public sealed class ModalDialogViewModel : BaseViewModel
{
    private TaskCompletionSource<DialogResult>? _tcs;

    private bool _isOpen;


    /// <summary>
    /// Gets or sets a value indicating whether the dialog is open.
    /// </summary>
    public bool IsOpen
    {
        get { return _isOpen; }
        set { SetProperty(ref _isOpen, value); }
    }

    /// <summary>
    /// Gets or sets the dialog title.
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the dialog message.
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the dialog buttons.
    /// </summary>
    public DialogButtons Buttons { get; set; }

    /// <summary>
    /// Gets the Ok command.
    /// </summary>
    public ICommand OkCommand { get; }

    /// <summary>
    /// Gets the Cancel command.
    /// </summary>
    public ICommand CancelCommand { get; }

    /// <summary>
    /// Gets the Yes command.
    /// </summary>
    public ICommand YesCommand { get; }

    /// <summary>
    /// Gets the No command.
    /// </summary>
    public ICommand NoCommand { get; }



#pragma warning disable CS8618 // To allow Design-time ctor branch
    /// <summary>
    /// Initializes a new instance of the <see cref="ModalDialogViewModel"/> class.
    /// </summary>
    public ModalDialogViewModel()
#pragma warning restore CS8618
    {
        if (DesignerProperties.GetIsInDesignMode(new DependencyObject()))
        {
            Title = "Sample Modal Dialog Title";
            Message = "This is an example of my modal dialog message area.\n" +
                "Style, theme, scale, and culture (language) are dynamic.\n\n" +
                "Button options include:\n" +
                " • Ok\n" +
                " • OkCancel\n" +
                " • YesNo\n" +
                " • YesNoCancel\n\n" +
                "This is repeated text to show how the dialog will wrap longer text. " +
                "This is repeated text to show how the dialog will wrap longer text.";
            IsOpen = true;
            Buttons = DialogButtons.YesNoCancel;
            return;
        }

        IsOpen = false;

        OkCommand = new RelayCommand(() => Close(DialogResult.Ok));
        CancelCommand = new RelayCommand(() => Close(DialogResult.Cancel));
        YesCommand = new RelayCommand(() => Close(DialogResult.Yes));
        NoCommand = new RelayCommand(() => Close(DialogResult.No));
    }



    /// <summary>
    /// Shows the modal dialog asynchronously.
    /// </summary>
    /// <param name="title">The dialog title.</param>
    /// <param name="message">The dialog message.</param>
    /// <param name="buttons">The dialog buttons.</param>
    /// <returns>The dialog result.</returns>
    public Task<DialogResult> ShowAsync(string title, string message, DialogButtons buttons)
    {
        Title = title ?? string.Empty;
        Message = message ?? string.Empty;
        Buttons = buttons;

        _tcs = new TaskCompletionSource<DialogResult>();
        IsOpen = true;
        OnPropertiesChanged(nameof(Title), nameof(Message), nameof(Buttons));

        return _tcs.Task;
    }


    private void Close(DialogResult result)
    {
        if (_tcs == null)
        {
            return;
        }

        IsOpen = false;
        var t = _tcs;
        _tcs = null;
        t.TrySetResult(result);
    }
}
