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

using TaskMaster.ViewModel.Dialog;

namespace TaskMaster.Presentation.UI;

/// <summary>
/// Service for showing in-app dialogs to the user.
/// </summary>
internal sealed class InAppDialogService : IDialogService
{
    private readonly ModalDialogViewModel _viewModel;

    /// <summary>
    /// Initializes a new instance of the <see cref="InAppDialogService"/> class.
    /// </summary>
    /// <param name="viewModel">The modal dialog view model.</param>
    public InAppDialogService(ModalDialogViewModel viewModel)
    {
        _viewModel = viewModel;
    }


    /// <summary>
    /// Simple OK dialog, for info presentation (no confirmation).
    /// </summary>
    /// <param name="title">The dialog title.</param>
    /// <param name="message">The dialog message.</param>
    /// <returns>Awaitable task with <see langword="true"/> once OK was pressed.</returns>
    public async Task<bool> InformationOkAsync(string title, string message)
    {
        _ = await ShowAsync(title, message);
        return true;
    }

    /// <summary>
    /// Simple OK confirmation dialog.
    /// </summary>
    /// <param name="title">The dialog title.</param>
    /// <param name="message">The dialog message.</param>
    /// <returns>Awaitable task with <see langword="true"/> if OK was pressed.</returns>
    public async Task<bool> ConfirmOkAsync(string title, string message)
    {
        var r = await ShowAsync(title, message, DialogButtons.OkCancel);
        return r == ViewModel.Dialog.DialogResult.Ok;
    }

    /// <summary>
    /// Simple Yes/No confirmation dialog.
    /// </summary>
    /// <param name="title">The dialog title.</param>
    /// <param name="message">The dialog message.</param>
    /// <returns>Awaitable task with <see langword="true"/> if Yes was pressed.</returns>
    public async Task<bool> ConfirmYesNoAsync(string title, string message)
    {
        var r = await ShowAsync(title, message, DialogButtons.YesNo);
        return r == ViewModel.Dialog.DialogResult.Yes;
    }


    /// <summary>
    /// Simple OK dialog, for info presentation (no confirmation).
    /// </summary>
    /// <param name="title">The dialog title.</param>
    /// <param name="message">The dialog message.</param>
    /// <returns>The dialog result.</returns>
    public Task<ViewModel.Dialog.DialogResult> ShowAsync(string title, string message)
    {
        return _viewModel.ShowAsync(title, message, DialogButtons.Ok);
    }

    /// <summary>
    /// Show a message box dialog with specified buttons.
    /// </summary>
    /// <param name="title">The dialog title.</param>
    /// <param name="message">The dialog message.</param>
    /// <param name="buttons">The dialog buttons.</param>
    /// <returns>The dialog result.</returns>
    public Task<ViewModel.Dialog.DialogResult> ShowAsync(string title, string message, DialogButtons buttons)
    {
        return _viewModel.ShowAsync(title, message, buttons);
    }
}
