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

using TaskMaster.Application.Composition;
using TaskMaster.Presentation.Commands;
using TaskMaster.Services;

namespace TaskMaster.ViewModel.Dialog;

/// <summary>
/// View model for the Support dialog.
/// </summary>
public sealed class SupportDialogViewModel : BaseViewModel
{
    private readonly IFeatureServices _features;

    private readonly RelayCommand _closeCommand;
    private readonly RelayCommand<string> _openUriCommand;

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
    /// Gets the command to close the dialog.
    /// </summary>
    public ICommand CloseCommand => _closeCommand;

    /// <summary>
    /// Gets the command to open a URI in the default browser.
    /// </summary>
    public ICommand OpenUriCommand => _openUriCommand;



#pragma warning disable CS8618 // To allow Design-time ctor branch
    /// <summary>
    /// Initializes a new instance of the <see cref="SupportDialogViewModel"/> class for design-time use.
    /// </summary>
    public SupportDialogViewModel()
    {
        if (DesignerProperties.GetIsInDesignMode(new DependencyObject()))
        {
            IsOpen = true;
        }
    }
#pragma warning restore CS8618

    /// <summary>
    /// Initializes a new instance of the <see cref="SupportDialogViewModel"/> class.
    /// </summary>
    public SupportDialogViewModel(IFeatureServices services)
    {
        _features = services;

        IsOpen = false;

        _openUriCommand = new RelayCommand<string>(OpenLink);

        _closeCommand = new RelayCommand(Close);
    }



    /// <summary>
    /// Opens the support dialog.
    /// </summary>
    public void Open()
    {
        IsOpen = true;
    }

    private void Close()
    {
        IsOpen = false;
    }

    private void OpenLink(string? url) => UriService.OpenLink(_features, url);
}

