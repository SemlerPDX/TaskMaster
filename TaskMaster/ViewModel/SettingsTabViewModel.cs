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
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Input;

using TaskMaster.Application;
using TaskMaster.Application.Composition;
using TaskMaster.Application.Features;
using TaskMaster.Model;
using TaskMaster.Presentation.Commands;
using TaskMaster.Presentation.Localization;
using TaskMaster.Presentation.UI;
using TaskMaster.Services;
using TaskMaster.ViewModel.Dialog;

namespace TaskMaster.ViewModel;

/// <summary>
/// The view model for the application's Settings tab.
/// </summary>
public sealed class SettingsTabViewModel : BaseViewModel, ITabAware
{
    private const string DownloadsUrl = @"https://github.com/SemlerPDX/TaskMaster/releases";

    private const string ChangedTitleKey = "Title.SettingsTab.Dialog.Changed";
    private const string ChangedMessageKey = "Message.SettingsTab.Dialog.Changed";

    private const string RestoreTitleKey = "Title.SettingsTab.Dialog.Restore";
    private const string RestoreMessageKey = "Message.SettingsTab.Dialog.Restore";

    private const string UninstallTitleKey = "Title.SettingsTab.Dialog.Uninstall";
    private const string UninstallMessageKey = "Message.SettingsTab.Dialog.Uninstall";

    private const string UninstalledTitleKey = "Title.SettingsTab.Dialog.Uninstalled";
    private const string UninstalledMessageKey = "Message.SettingsTab.Dialog.Uninstalled";

    private const string RestartRequiredTitleKey = "Title.SettingsTab.Dialog.RestartRequired";
    private const string EnabledRestartMessageKey = "Message.SettingsTab.Dialog.EnabledRestart";
    private const string DisabledRestartMessageKey = "Message.SettingsTab.Dialog.DisabledRestart";

    private const string SavedMessageKey = "Message.SettingsTab.History.Saved";
    private const string CanceledMessageKey = "Message.SettingsTab.History.Canceled";
    private const string RestoredMessageKey = "Message.SettingsTab.History.Restored";
    private const string AsAdminStateMessageKey = "Message.SettingsTab.History.RunAsAdmin";

    private const string UpdateFoundTitleKey = "Title.SettingsTab.Dialog.UpdateFound";
    private const string UpdateFoundMessageKey = "Message.SettingsTab.Dialog.UpdateFound";

    private const string UpdateNotFoundTitleKey = "Title.SettingsTab.Dialog.UpdateNotFound";
    private const string UpdateNotFoundMessageKey = "Message.SettingsTab.Dialog.UpdateNotFound";

    private const string UpdateErrorTitleKey = "Title.SettingsTab.Dialog.UpdateError";
    private const string UpdateErrorMessageKey = "Message.SettingsTab.Dialog.UpdateError";

    private const int MinUiScale = 50;
    private const int MaxUiScale = 200;
    private const int UiScaleStep = 5;


    private readonly AboutDialogViewModel _appAboutDialogViewModel;
    private readonly LicenseDialogViewModel _appLicenseDialogViewModel;
    private readonly SupportDialogViewModel _appSupportDialogViewModel;
    private readonly IFeatureServices _features;
    private readonly IShellServices _services;

    private readonly RelayCommand _saveSettingsCommand;
    private readonly RelayCommand _cancelSettingsCommand;
    private readonly RelayCommand _checkUpdatesCommand;
    private readonly RelayCommand _restoreSettingsCommand;
    private readonly RelayCommand _uninstallCommand;

    private readonly bool _isFirstUse;

    private bool _saveCancelEnabled;
    private bool _isCheckingUpdates;
    private bool _isRestoring;
    private bool _isFactoryResetting;
    private bool _notPendingRestart;
    private bool _startupSettingsHandled;


    /// <summary>
    /// Available corner style options for buttons..<br/>
    /// </summary>
    public IReadOnlyList<StyleOption> StyleOptions { get; } = [.. StyleCatalog.StyleOptions];

    /// <summary>
    /// Available application color palette themes.
    /// </summary>
    public IReadOnlyList<string> ThemeOptions { get; } = [.. ThemeCatalog.ThemeOptions];

    /// <summary>
    /// UI Scale options from Max to Min in steps.
    /// </summary>
    public IReadOnlyList<int> UiScaleOptions { get; } =
        [.. Enumerable.Range(0, ((MaxUiScale - MinUiScale) / UiScaleStep) + 1)
                      .Select(i => MinUiScale + (i * UiScaleStep))
                      .Reverse()];

    /// <summary>
    /// Supported languages for the UI.
    /// </summary>
    public IReadOnlyList<LangItem> Languages { get; } = [.. LocalizationCatalog.Languages.Reverse()];

    /// <summary>
    /// The working copy of settings being edited.
    /// </summary>
    public SettingsEditor WorkingCopy { get; private set; }

    /// <summary>
    /// The former working copy of settings for change tracking.
    /// </summary>
    public SettingsData FormerWorkingCopy { get; private set; }

    /// <summary>
    /// Indicates whether there are unsaved changes to settings.
    /// </summary>
    public bool SaveCancelEnabled
    {
        get => _saveCancelEnabled;
        private set
        {
            if (SetProperty(ref _saveCancelEnabled, value))
            {
                _saveSettingsCommand?.RaiseCanExecuteChanged();
                _cancelSettingsCommand?.RaiseCanExecuteChanged();
            }
        }
    }

    /// <summary>
    /// Indicates whether a restart is pending to apply certain settings changes.<br/>
    /// <see langword="True"/> when a restart is NOT pending.
    /// </summary>
    public bool NotPendingRestart
    {
        get { return _notPendingRestart; }
        set { SetProperty(ref _notPendingRestart, value); }
    }


    /// <summary>
    /// Gets the command to save settings.
    /// </summary>
    public ICommand SaveSettingsCommand => _saveSettingsCommand;

    /// <summary>
    /// Gets the command to cancel settings changes.
    /// </summary>
    public ICommand CancelSettingsCommand => _cancelSettingsCommand;

    /// <summary>
    /// Gets the command to open the Support dialog.
    /// </summary>
    public ICommand OpenSupport { get; }

    /// <summary>
    /// Gets the command to open the About dialog.
    /// </summary>
    public ICommand OpenAboutCommand { get; }

    /// <summary>
    /// Gets the command to show the License dialog.
    /// </summary>
    public ICommand ShowLicenseCommand { get; }

    /// <summary>
    /// Gets the command to check for application updates.
    /// </summary>
    public ICommand CheckUpdatesCommand => _checkUpdatesCommand;

    /// <summary>
    /// Gets the command to restore default settings.
    /// </summary>
    public ICommand RestoreSettingsCommand => _restoreSettingsCommand;

    /// <summary>
    /// Gets the command to factory reset (uninstall) the application.
    /// </summary>
    public ICommand UninstallCommand => _uninstallCommand;



#pragma warning disable CS8618 // To allow Design-time ctor branch
    /// <summary>
    /// Initializes a new instance of the <see cref="SettingsTabViewModel"/> class for design-time use.
    /// </summary>
    public SettingsTabViewModel()
    {
        if (DesignerProperties.GetIsInDesignMode(new DependencyObject()))
        {
            SaveCancelEnabled = false;
            NotPendingRestart = false; // to show example of disabled appearance

            WorkingCopy = new SettingsEditor(new SettingsData());

            var styleService = new StyleService();
            int radius = styleService.CurrentStyle * WorkingCopy.CornerRadiusFactor;
            DesignTokenUpdater.SetCornerRadiusStyle(radius);
        }
    }
#pragma warning restore CS8618

    /// <summary>
    /// Initializes a new instance of the <see cref="SettingsTabViewModel"/> class.
    /// </summary>
    /// <param name="appAboutDialogViewModel">The About dialog view model.</param>
    /// <param name="appLicenseDialogViewModel">The License dialog view model.</param>
    /// <param name="appSupportDialogViewModel">The Support dialog view model.</param>
    /// <param name="features">The feature services.</param>
    /// <param name="services">The shell services.</param>
    public SettingsTabViewModel(
        AboutDialogViewModel appAboutDialogViewModel,
        LicenseDialogViewModel appLicenseDialogViewModel,
        SupportDialogViewModel appSupportDialogViewModel,
        IFeatureServices features,
        IShellServices services)
    {
        _appAboutDialogViewModel = appAboutDialogViewModel;
        _appLicenseDialogViewModel = appLicenseDialogViewModel;
        _appSupportDialogViewModel = appSupportDialogViewModel;
        _features = features;
        _services = services;

        _isFirstUse = InitializeSettings();
        WorkingCopy = new SettingsEditor(_features.Settings.Current);

        // Tracking runtime-visible changes so cancel can revert them
        FormerWorkingCopy = WorkingCopy.ToSettingsData();

        WorkingCopy.PropertyChanged += OnWorkingCopyPropertyChanged;

        RecomputeDirty();

        _saveSettingsCommand = new RelayCommand(Save, () => SaveCancelEnabled);
        _cancelSettingsCommand = new RelayCommand(Cancel, () => SaveCancelEnabled);

        _checkUpdatesCommand = new RelayCommand(CheckUpdates, () => !_isCheckingUpdates);
        _restoreSettingsCommand = new RelayCommand(RestoreSettings, () => !_isRestoring);
        _uninstallCommand = new RelayCommand(Uninstall, () => !_isFactoryResetting);

        OpenSupport = new RelayCommand(ShowSupport);
        OpenAboutCommand = new RelayCommand(ShowAbout);
        ShowLicenseCommand = new RelayCommand(ShowLicense);

        if (_isFirstUse)
        {
            // Persist initial settings
            Save();
        }

        PrintPermissionStatus();
        NotPendingRestart = true;
    }


    void ITabAware.OnActivated()
    {
        OnViewActivated();
    }

    void ITabAware.OnDeactivated()
    {
        OnViewDeactivated();
    }


    private void Save()
    {
        _ = HandleApplicationSettings();

        FormerWorkingCopy = WorkingCopy.ToSettingsData();
        _features.Settings.Apply(WorkingCopy.ToSettingsData());

        LogHistory(SavedMessageKey);
        RecomputeDirty(); // now equal => disabled
    }

    private void Cancel()
    {
        WorkingCopy.PropertyChanged -= OnWorkingCopyPropertyChanged;
        WorkingCopy = new SettingsEditor(_features.Settings.Current);
        WorkingCopy.PropertyChanged += OnWorkingCopyPropertyChanged;

        var hasChanged = !StructuralEquals(WorkingCopy.ToSettingsData(), FormerWorkingCopy);

        if (hasChanged)
        {
            if (WorkingCopy.CornerRadiusFactor != FormerWorkingCopy.CornerRadiusFactor)
            {
                _services.Style.ApplyStyle(FormerWorkingCopy.CornerRadiusFactor);
            }

            // Writeback in persisted settings if either changed during runtime
            WorkingCopy.PropertyChanged -= OnWorkingCopyPropertyChanged;
            WorkingCopy = new SettingsEditor(FormerWorkingCopy);
            WorkingCopy.PropertyChanged += OnWorkingCopyPropertyChanged;

            _features.Settings.Apply(WorkingCopy.ToSettingsData());
        }

        OnPropertyChanged(nameof(WorkingCopy));
        LogHistory(CanceledMessageKey);
        RecomputeDirty();
    }

    private void LoadDefaultSettings()
    {
        WorkingCopy.PropertyChanged -= OnWorkingCopyPropertyChanged;
        WorkingCopy = new SettingsEditor(new SettingsData())
        {
            // brand-new defaults, retaining AppGuid with proper new app dir path - bypasses new culture dialog on next startup
            AppGuid = _features.Settings.Current.AppGuid,
            AppDirectory = Path.GetDirectoryName(AppInfo.Location) ?? AppDomain.CurrentDomain.BaseDirectory,
            ConfigFilePath = _features.Config.ConfigPath,
            LogFilePath = _features.Settings.Current.LogFilePath
        };

        ApplyStartupChanges();

        FormerWorkingCopy = WorkingCopy.ToSettingsData();

        _services.Style.ApplyStyle(WorkingCopy.CornerRadiusFactor);

        WorkingCopy.PropertyChanged += OnWorkingCopyPropertyChanged;
        OnPropertyChanged(nameof(WorkingCopy));

        _features.Settings.Apply(WorkingCopy.ToSettingsData());

        LogHistory(RestoredMessageKey);
        RecomputeDirty();
    }

    private void CheckUpdates()
    {
        if (_isCheckingUpdates)
        {
            return;
        }

        _isCheckingUpdates = true;
        _checkUpdatesCommand.RaiseCanExecuteChanged();

        _ = Task.Run(async () =>
        {
            try
            {
                var updateData = await UpdateChecker.CheckForUpdatesAsync(_features.Settings.Current.UiCulture);

                var title = string.Empty;
                var message = string.Empty;

                if (updateData == null)
                {
                    title = _features.Localization.GetString(UpdateErrorTitleKey);
                    message = _features.Localization.GetString(UpdateErrorMessageKey);
                }
                else if (updateData.Length == 1)
                {
                    title = _features.Localization.GetString(UpdateNotFoundTitleKey);
                    var messageBase = _features.Localization.GetString(UpdateNotFoundMessageKey);
                    message = string.Format(messageBase, updateData[0]);
                }
                else if (updateData.Length == 2)
                {
                    title = _features.Localization.GetString(UpdateFoundTitleKey);
                    var messageBase = _features.Localization.GetString(UpdateFoundMessageKey);
                    message = string.Format(messageBase, updateData[0], updateData[1]);
                }

                var ok = await AppDialog.Current.ConfirmOkAsync(title, message);
                if (ok)
                {
                    UriService.OpenLink(_features, DownloadsUrl);
                }
            }
            catch // any logging performed at the UpdateChecker level
            {
                var title = _features.Localization.GetString(UpdateErrorTitleKey);
                var message = _features.Localization.GetString(UpdateErrorMessageKey);
                var ok = await AppDialog.Current.InformationOkAsync(title, message);
            }
            finally
            {
                App.Current.Dispatcher.Invoke(() =>
                {
                    _isCheckingUpdates = false;
                    _checkUpdatesCommand.RaiseCanExecuteChanged();
                });
            }
        });
    }


    private void RestoreSettings()
    {
        RunWithLatch(ref _isRestoring, _restoreSettingsCommand, () =>
        {
            _ = App.Current.Dispatcher.InvokeAsync(async () =>
            {
                string title = _features.Localization.GetString(RestoreTitleKey);
                string message = _features.Localization.GetString(RestoreMessageKey);
                await ConfirmAndRunAsync(title, message, LoadDefaultSettings);
            });
        });
    }

    private void Uninstall()
    {
        RunWithLatch(ref _isFactoryResetting, _uninstallCommand, () =>
        {
            _ = App.Current.Dispatcher.InvokeAsync(async () =>
            {
                string title = _features.Localization.GetString(UninstallTitleKey);
                string message = _features.Localization.GetString(UninstallMessageKey);
                await ConfirmAndRunAsync(title, message, () => _ = ResetAndShutdown());
            });
        });
    }

    private void ShowAbout() => _appAboutDialogViewModel.Open();
    private void ShowLicense() => _appLicenseDialogViewModel.Open();
    private void ShowSupport() => _appSupportDialogViewModel.Open();


    private bool InitializeSettings()
    {
        var appDir = Path.GetDirectoryName(AppInfo.Location) ?? AppDomain.CurrentDomain.BaseDirectory;

        if (_features.Settings.IsFirstTimeSetup)
        {
            _features.Settings.Current.AppGuid = Guid.NewGuid().ToString();
            _features.Settings.Current.ConfigFilePath = _features.Config.ConfigPath;

            var logFolder = Path.GetDirectoryName(_features.Config.ConfigPath) ?? appDir;
            _features.Settings.Current.LogFilePath = Path.Combine(logFolder, _features.Settings.Current.LogFilePath);
        }

        // Restore app dir if changed (moved install, reinstall, new install, etc)
        var isGoodAppDirPath = string.Equals(appDir, _features.Settings.Current.AppDirectory, StringComparison.OrdinalIgnoreCase);

        if (!isGoodAppDirPath)
        {
            _features.Settings.Current.AppDirectory = appDir;

            if (ScheduledTaskHandler.Exists(AppInfo.Name))
            {
                // Re-apply startup settings if app dir changed and a startup task exists
                StartupSettingsHandler.Apply(
                    AppInfo.Location,
                    AppInfo.Name,
                    _features.Settings.Current.RunAsAdmin,
                    _features.Settings.Current.StartWithWindows);
            }

            return true;
        }

        return _features.Settings.IsFirstTimeSetup;
    }

    private void RecomputeDirty()
    {
        SaveCancelEnabled = !StructuralEquals(WorkingCopy.ToSettingsData(), FormerWorkingCopy);
    }

    private static bool StructuralEquals(SettingsData a, SettingsData b)
    {
        var props = typeof(SettingsData).GetProperties(BindingFlags.Public | BindingFlags.Instance);
        foreach (var p in props)
        {
            if (!p.CanRead)
            {
                continue;
            }

            var valueA = p.GetValue(a);
            var valueB = p.GetValue(b);
            if (!Equals(valueA, valueB))
            {
                return false;
            }
        }

        return true;
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0059:Unnecessary assignment of a value",
        Justification = "Setting the re-entrancy latch via ref updates caller-visible state.")]
    private static void RunWithLatch(ref bool latch, RelayCommand cmd, Action work)
    {
        if (latch)
        {
            return;
        }

        latch = true; // hold busy flag during work
        cmd.RaiseCanExecuteChanged();

        try
        {
            work();
        }
        finally
        {
            latch = false;
            cmd.RaiseCanExecuteChanged();
        }
    }

    private static async Task ConfirmAndRunAsync(string title, string message, Action onConfirmed)
    {
        bool confirmed = await AppDialog.Current.ConfirmYesNoAsync(title, message);
        if (!confirmed)
        {
            return;
        }

        onConfirmed();
    }

    private async Task<bool> ConfirmOrCancelChanges()
    {
        string title = _features.Localization.GetString(ChangedTitleKey);
        string message = _features.Localization.GetString(ChangedMessageKey);

        return await AppDialog.Current.ConfirmYesNoAsync(title, message);
    }

    private async Task ResetAndShutdown()
    {
        WorkingCopy.PropertyChanged -= OnWorkingCopyPropertyChanged;
        WorkingCopy = new SettingsEditor(new SettingsData());

        // Remove any startup run entry or task for this app
        var appDir = Path.GetDirectoryName(AppInfo.Location) ?? AppDomain.CurrentDomain.BaseDirectory;
        var exePath = AppInfo.Location ?? Path.Combine(appDir, AppInfo.Name + ".exe");
        StartupSettingsHandler.Apply(exePath, AppInfo.Name, runAsAdministrator: false, startWithWindows: false);

        // Register cleared data to signal to any/all classes that reset occurred prior to close
        _features.Settings.Apply(WorkingCopy.ToSettingsData());

        _features.Settings.DeleteAll();
        _features.Config.DeleteAll();

        var title = _features.Localization.GetString(UninstalledTitleKey);
        var messageBase = _features.Localization.GetString(UninstalledMessageKey);
        var message = string.Format(messageBase, appDir);


        var ok = await AppDialog.Current.InformationOkAsync(title, message);

        if (!string.IsNullOrEmpty(appDir) && Directory.Exists(appDir))
        {
            OpenFolder(appDir);
        }

        Thread.Sleep(1000);

        App.Current.Dispatcher.Invoke(() =>
        {
            App.Current.Shutdown();
        });
    }

    private static void OpenFolder(string folderPath)
    {
        Process.Start("explorer.exe", folderPath);
    }

    private void PrintPermissionStatus()
    {
        if (!AppInfo.IsElevated)
        {
            return;
        }

        LogHistory(AsAdminStateMessageKey);
    }

    private async Task HandleApplicationSettings()
    {
        _startupSettingsHandled = false;

        var isStartWithWindowsChanged = (WorkingCopy.StartWithWindows != _features.Settings.Current.StartWithWindows);
        var isRunAsAdminChanged = (WorkingCopy.RunAsAdmin != AppInfo.IsElevated) ||
            (WorkingCopy.RunAsAdmin != _features.Settings.Current.RunAsAdmin);

        if (!isStartWithWindowsChanged && !isRunAsAdminChanged)
        {
            return;
        }

        ApplyStartupChanges();

        await HandleRunAsAdminChanged();
    }

    private void ApplyStartupChanges()
    {
        var isStartWithWindowsChanged = (WorkingCopy.StartWithWindows != _features.Settings.Current.StartWithWindows);
        if (!isStartWithWindowsChanged)
        {
            return;
        }

        var exePath = AppInfo.Location ?? Path.Combine(WorkingCopy.AppDirectory, AppInfo.Name + ".exe");
        bool asAdmin = (WorkingCopy.RunAsAdmin && AppInfo.IsElevated);
        StartupSettingsHandler.Apply(exePath, AppInfo.Name, asAdmin, WorkingCopy.StartWithWindows);
        _startupSettingsHandled = true;
    }

    private async Task HandleRunAsAdminChanged()
    {
        var isRunAsAdminChanged = (WorkingCopy.RunAsAdmin != AppInfo.IsElevated) ||
            (WorkingCopy.RunAsAdmin != _features.Settings.Current.RunAsAdmin);

        if (!isRunAsAdminChanged)
        {
            return;
        }

        var isPendingRunAsAdmin = WorkingCopy.RunAsAdmin != AppInfo.IsElevated;
        if (isPendingRunAsAdmin)
        {
            // Remove any task used to elevate on startup if present while still elevated
            if (!_startupSettingsHandled && !WorkingCopy.RunAsAdmin && AppInfo.IsElevated)
            {
                var exePath = AppInfo.Location ?? Path.Combine(WorkingCopy.AppDirectory, AppInfo.Name + ".exe");
                StartupSettingsHandler.Apply(exePath, AppInfo.Name, false, false);
            }

            // Show dialog explaining required restart if RunAsAdmin changed
            var title = _features.Localization.GetString(RestartRequiredTitleKey);
            var enabledMessage = _features.Localization.GetString(EnabledRestartMessageKey);
            var disabledMessage = _features.Localization.GetString(DisabledRestartMessageKey);
            var message = WorkingCopy.RunAsAdmin ? enabledMessage : disabledMessage;

            _ = await AppDialog.Current.InformationOkAsync(title, message);

            NotPendingRestart = false;
        }
    }


    private void LogHistory(string key) => LogHistory(key, null, []);
    private void LogHistory(string key, string? message = null, params object[] args)
    {
        InfoService.LogLauncherHistory(_features, key, message, args);
        InfoService.LogKillerHistory(_features, key, message, args);
    }


    private void OnViewActivated()
    {
        if (!_isFirstUse)
        {
            return;
        }

        WorkingCopy.PropertyChanged -= OnWorkingCopyPropertyChanged;

        // Sync with first-time use selection in Culture Dialog
        WorkingCopy.UiCulture = _features.Settings.Current.UiCulture;
        FormerWorkingCopy = WorkingCopy.ToSettingsData();

        WorkingCopy.PropertyChanged += OnWorkingCopyPropertyChanged;
    }

    private void OnViewDeactivated()
    {
        if (!SaveCancelEnabled)
        {
            return;
        }

        _ = App.Current.Dispatcher.InvokeAsync(async () =>
        {
            bool saveChanges = await ConfirmOrCancelChanges();
            if (saveChanges)
            {
                Save();
            }
            else
            {
                Cancel();
            }
        });
    }


    private void OnWorkingCopyPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        // If the persisted factor changes, raise the computed radius, too.
        if (e.PropertyName == nameof(SettingsEditor.CornerRadiusFactor))
        {
            _services.Style.ApplyStyle(WorkingCopy.CornerRadiusFactor);
            _features.Settings.Apply(WorkingCopy.ToSettingsData());
        }

        // If the theme changes, apply live theme preview
        if (e.PropertyName == nameof(SettingsEditor.Theme))
        {
            _features.Settings.Apply(WorkingCopy.ToSettingsData());
        }

        // If the ui scale changes, apply live UI scale preview
        if (e.PropertyName == nameof(SettingsEditor.UiScalePercent))
        {
            _features.Settings.Apply(WorkingCopy.ToSettingsData());
        }

        // If the ui culture changes, apply live language preview
        if (e.PropertyName == nameof(SettingsEditor.UiCulture))
        {
            _features.Settings.Apply(WorkingCopy.ToSettingsData());
        }

        RecomputeDirty();
    }


    /// <summary>
    /// Call this cleanup from the hosting Tab control when the tab is closing.<br/>
    /// Checks for changes and presents dialog for user to save or cancel.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task OnClosingAsync()
    {
        WorkingCopy.PropertyChanged -= OnWorkingCopyPropertyChanged;

        if (!SaveCancelEnabled)
        {
            return;
        }

        string title = _features.Localization.GetString(ChangedTitleKey);
        string message = _features.Localization.GetString(ChangedMessageKey);

        bool confirmed = await AppDialog.Current.ConfirmYesNoAsync(title, message);
        if (confirmed)
        {
            Save();
        }
        else
        {
            Cancel();
        }
    }
}
