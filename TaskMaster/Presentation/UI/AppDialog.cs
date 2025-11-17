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
/// Static access point for the current dialog service implementation
/// below so existing VMs can call without wiring DI everywhere.
/// </summary>
internal static class AppDialog
{
    /// <summary>
    /// The current dialog service implementation.
    /// </summary>
    public static IDialogService Current { get; set; } = new MessageBoxDialogService();
}

/// <summary>
/// Dialog service implementation that uses WPF MessageBox.<br/>
/// Can fallback to WPF MessageBox if no overlay host is registered yet.
/// </summary>
internal sealed class MessageBoxDialogService : IDialogService
{

    /// <summary>
    /// Simple OK dialog, for info presentation (no confirmation).
    /// </summary>
    /// <param name="title"></param>
    /// <param name="message"></param>
    /// <returns>Awaitable task with <see langword="true"/> when dismissed.</returns>
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
    /// <returns>Merely a completed task.</returns>
    public async Task<ViewModel.Dialog.DialogResult> ShowAsync(string title, string message)
    {
        _ = await ShowAsync(title, message, DialogButtons.Ok);
        return ViewModel.Dialog.DialogResult.Ok;
    }

    /// <summary>
    /// Show a message box dialog with specified buttons.<br/>
    /// Uses custom view if available, otherwise falls back to WPF MessageBox.
    /// </summary>
    /// <param name="title">The dialog title.</param>
    /// <param name="message">The dialog message.</param>
    /// <param name="buttons">The dialog buttons.</param>
    /// <returns>The dialog result.</returns>
    public Task<ViewModel.Dialog.DialogResult> ShowAsync(string title, string message, DialogButtons buttons)
    {
        var dispatcher = System.Windows.Application.Current?.Dispatcher;
        if (dispatcher == null || dispatcher.CheckAccess())
        {
            var button = GetButton(buttons);
            var result = System.Windows.MessageBox.Show(message, title, button, System.Windows.MessageBoxImage.Information);
            var mapped = MappedResult(result);

            return Task.FromResult(mapped);
        }
        else
        {
            return dispatcher.InvokeAsync(() =>
            {
                var button = GetButton(buttons);
                var result = System.Windows.MessageBox.Show(message, title, button, System.Windows.MessageBoxImage.Information);

                return MappedResult(result);
            }).Task;
        }
    }


    private static System.Windows.MessageBoxButton GetButton(DialogButtons buttons)
    {
        return
            buttons == DialogButtons.Ok ? System.Windows.MessageBoxButton.OK :
            buttons == DialogButtons.OkCancel ? System.Windows.MessageBoxButton.OKCancel :
            buttons == DialogButtons.YesNo ? System.Windows.MessageBoxButton.YesNo :
            System.Windows.MessageBoxButton.YesNoCancel;
    }

    private static ViewModel.Dialog.DialogResult MappedResult(System.Windows.MessageBoxResult result)
    {
        return
            result == System.Windows.MessageBoxResult.OK ? ViewModel.Dialog.DialogResult.Ok :
            result == System.Windows.MessageBoxResult.Yes ? ViewModel.Dialog.DialogResult.Yes :
            result == System.Windows.MessageBoxResult.No ? ViewModel.Dialog.DialogResult.No :
            ViewModel.Dialog.DialogResult.Cancel;
    }
}
