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
using TaskMaster.Services;

namespace TaskMaster.ViewModel.Dialog;

/// <summary>
/// View model for the application license dialog.
/// </summary>
public sealed class LicenseDialogViewModel : BaseViewModel
{
    private readonly RelayCommand _closeCommand;

    private bool _isOpen;


    /// <summary>
    /// Gets the license lines for display in the License dialog.
    /// </summary>
    public IReadOnlyList<string> LicenseLines { get; private set; } = [];

    /// <summary>
    /// Gets or sets a value indicating whether the dialog is open.
    /// </summary>
    public bool IsOpen
    {
        get { return _isOpen; }
        set { SetProperty(ref _isOpen, value); }
    }


    /// <summary>
    /// Gets the command to close the dialog.
    /// </summary>
    public ICommand CloseCommand => _closeCommand;



#pragma warning disable CS8618 // To allow Design-time ctor branch
    /// <summary>
    /// Initializes a new instance of the <see cref="LicenseDialogViewModel"/> class.
    /// </summary>
    public LicenseDialogViewModel()
#pragma warning restore CS8618
    {
        RefreshLicenseLines();

        if (DesignerProperties.GetIsInDesignMode(new DependencyObject()))
        {
            IsOpen = true;
            return;
        }

        IsOpen = false;

        _closeCommand = new RelayCommand(Close);
    }



    /// <summary>
    /// Opens the license dialog.
    /// </summary>
    public void Open()
    {
        IsOpen = true;
    }

    /// <summary>
    /// Refreshes the license lines from the license service.
    /// </summary>
    public void RefreshLicenseLines()
    {
        var text = Task.Run(async () => await LicenseService.GetLicenseTextAsync()).Result;

        // Split on CRLF or LF; keep empty lines so spacing is preserved
        LicenseLines = text.Split(["\r\n", "\n"], StringSplitOptions.None);

        OnPropertyChanged(nameof(LicenseLines));
    }


    private void Close()
    {
        IsOpen = false;
    }

}
