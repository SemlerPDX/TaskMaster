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

using TaskMaster.Application;
using TaskMaster.Application.Composition;
using TaskMaster.Presentation.Commands;
using TaskMaster.Services;

namespace TaskMaster.ViewModel.Dialog;

/// <summary>
/// View model for the About dialog.
/// </summary>
public sealed class AboutDialogViewModel : BaseViewModel
{
    private readonly LicenseDialogViewModel _appLicenseDialogViewModel;
    private readonly IFeatureServices _features;

    private readonly RelayCommand _closeCommand;
    private readonly RelayCommand _showLicenseCommand;
    private readonly RelayCommand<string> _openUriCommand;

    private string _appVersion;
    private bool _isOpen;


    /// <summary>
    /// Gets the disclaimer lines for display in the About dialog.
    /// </summary>
    public IReadOnlyList<string> DisclaimerLines { get; private set; } = [];

    /// <summary>
    /// Gets or sets the app version string.
    /// </summary>
    public string AppVersion
    {
        get { return _appVersion; }
        set { SetProperty(ref _appVersion, value); }
    }

    /// <summary>
    /// Gets or sets a value indicating whether the About dialog is open.
    /// </summary>
    public bool IsOpen
    {
        get { return _isOpen; }
        set { SetProperty(ref _isOpen, value); }
    }


    /// <summary>
    /// Gets the command to close the About dialog.
    /// </summary>
    public ICommand CloseCommand => _closeCommand;

    /// <summary>
    /// Gets the command to show the app license dialog.
    /// </summary>
    public ICommand ShowLicenseCommand => _showLicenseCommand;

    /// <summary>
    /// Gets the command to open a URI in the default web browser.
    /// </summary>
    public ICommand OpenUriCommand => _openUriCommand;



#pragma warning disable CS8618 // To allow Design-time ctor branch
    /// <summary>
    /// Initializes a new instance of the <see cref="AboutDialogViewModel"/> class for design-time use.
    /// </summary>
    public AboutDialogViewModel()
    {
        if (DesignerProperties.GetIsInDesignMode(new DependencyObject()))
        {
            _ = LoadDisclaimerLinesAsync();

            _isOpen = true;
            _appVersion = AppInfo.VersionString;
        }
    }
#pragma warning restore CS8618

    /// <summary>
    /// Initializes a new instance of the <see cref="AboutDialogViewModel"/> class.
    /// </summary>
    /// <param name="appLicenseDialogViewModel">The app license dialog view model.</param>
    /// <param name="features">The feature services.</param>
    public AboutDialogViewModel(
        LicenseDialogViewModel appLicenseDialogViewModel,
        IFeatureServices features)
    {
        _appLicenseDialogViewModel = appLicenseDialogViewModel;
        _features = features;

        _ = LoadDisclaimerLinesAsync();

        _isOpen = false;

        _appVersion = AppInfo.VersionString;

        _closeCommand = new RelayCommand(Close);
        _showLicenseCommand = new RelayCommand(ShowLicense);
        _openUriCommand = new RelayCommand<string>(OpenLink);
    }



    /// <summary>
    /// Loads the disclaimer lines asynchronously.
    /// </summary>
    /// <returns>Task representing the asynchronous operation.</returns>
    public async Task LoadDisclaimerLinesAsync()
    {
        var text = await LicenseService.GetDisclaimerTextAsync();
        DisclaimerLines = text.TrimEnd().Split(["\r\n", "\n"], StringSplitOptions.None);
        OnPropertyChanged(nameof(DisclaimerLines));
    }

    /// <summary>
    /// Opens the About dialog.
    /// </summary>
    public void Open()
    {
        IsOpen = true;
    }


    private void Close()
    {
        IsOpen = false;
    }

    private void ShowLicense()
    {
        _appLicenseDialogViewModel.Open();
        Close();
    }

    private void OpenLink(string? url) => UriService.OpenLink(_features, url);
}

