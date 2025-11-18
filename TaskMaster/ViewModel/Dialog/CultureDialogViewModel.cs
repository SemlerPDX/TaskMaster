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
using TaskMaster.Presentation.Localization;
using TaskMaster.Services;

namespace TaskMaster.ViewModel.Dialog;

/// <summary>
/// View model for the Culture selection dialog.
/// </summary>
public sealed class CultureDialogViewModel : BaseViewModel
{
    private const string DefaultCulture = "en-US";

    private const string WelcomeMessageKey = "Message.CultureDialog.History.Welcome";
    private const string ReviewSettingsMessageKey = "Message.CultureDialog.History.ReviewSettings";
    private const string RestoreDefaultsMessageKey = "Message.CultureDialog.History.RestoreDefaults";
    private const string UninstallMessageKey = "Message.CultureDialog.History.Uninstall";
    private const string HelpMessageKey = "Message.CultureDialog.History.Help";


    private readonly AboutDialogViewModel _aboutDialogViewModel;
    private readonly IFeatureServices _features;

    private bool _isOpen;
    private LangItem? _selectedLanguage;


    /// <summary>
    /// Gets or sets a value indicating whether the culture dialog is open.
    /// </summary>
    public bool IsOpen
    {
        get { return _isOpen; }
        set { SetProperty(ref _isOpen, value); }
    }

    /// <summary>
    /// Gets the list of available languages.
    /// </summary>
    public IReadOnlyList<LangItem> Languages { get; }

    /// <summary>
    /// Gets or sets the selected language.
    /// </summary>
    public LangItem? SelectedLanguage
    {
        get { return _selectedLanguage; }
        set
        {
            SetProperty(ref _selectedLanguage, value);
            ApplyPreview();
        }
    }

    /// <summary>
    /// Gets the command to apply the selected culture and close the dialog.
    /// </summary>
    public ICommand OkCommand { get; }



#pragma warning disable CS8618 // To allow Design-time ctor branch
    /// <summary>
    /// Initializes a new instance of the <see cref="CultureDialogViewModel"/> class for design-time use.
    /// </summary>
    public CultureDialogViewModel()
    {
        if (DesignerProperties.GetIsInDesignMode(new DependencyObject()))
        {
            IsOpen = true;
            Languages = LocalizationCatalog.Languages;
        }
    }
#pragma warning restore CS8618

    /// <summary>
    /// Initializes a new instance of the <see cref="CultureDialogViewModel"/> class.
    /// </summary>
    /// <param name="aboutDialogViewModel">The about dialog view model.</param>
    /// <param name="features">The feature services.</param>
    public CultureDialogViewModel(
        AboutDialogViewModel aboutDialogViewModel,
        IFeatureServices features)
    {
        _aboutDialogViewModel = aboutDialogViewModel;
        _features = features;

        IsOpen = false;

        Languages = LocalizationCatalog.Languages;

        // Pre-select current or default language
        var current = _features.Settings.Current?.UiCulture ?? DefaultCulture;
        SelectedLanguage = Languages.FirstOrDefault(l => string.Equals(l.Culture, current, StringComparison.OrdinalIgnoreCase));

        OkCommand = new RelayCommand(ApplyAndClose, CanApply);
    }



    /// <summary>
    /// Opens the culture dialog.
    /// </summary>
    public void Open()
    {
        IsOpen = true;
    }


    private bool CanApply()
    {
        return SelectedLanguage != null;
    }

    private void ApplyPreview()
    {
        if (SelectedLanguage == null)
        {
            return;
        }

        _features.Localization.ApplyCulture(SelectedLanguage.Culture);
    }

    private void ApplyAndClose()
    {
        if (SelectedLanguage == null)
        {
            return;
        }

        // Persist the selection; SettingsStore will raise SettingsApplied
        _features.Settings.Current.UiCulture = SelectedLanguage.Culture;
        _features.Settings.Apply(_features.Settings.Current);

        PrintNewUserMessages();

        _aboutDialogViewModel.Open();
        IsOpen = false;
    }

    private void PrintNewUserMessages()
    {
        LogHistory(WelcomeMessageKey);
        LogHistory(ReviewSettingsMessageKey);
        LogHistory(RestoreDefaultsMessageKey);
        LogHistory(UninstallMessageKey);
        LogHistory(HelpMessageKey);

    }

    private void LogHistory(string key)
    {
        InfoService.LogLauncherHistory(_features, key);
        InfoService.LogKillerHistory(_features, key);
    }
}
