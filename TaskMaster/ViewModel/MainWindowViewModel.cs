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
using TaskMaster.Infrastructure.Persistence;
using TaskMaster.Presentation.Commands;
using TaskMaster.Presentation.UI;
using TaskMaster.ViewModel.Dialog;

namespace TaskMaster.ViewModel;


/// <summary>
/// Enumeration of the main application tabs.
/// </summary>
public enum AppTab
{
    /// <summary>
    /// The Task Launcher tab.
    /// </summary>
    TaskLauncher = 0,

    /// <summary>
    /// The Task Killer tab.
    /// </summary>
    TaskKiller = 1,

    /// <summary>
    /// The Settings tab.
    /// </summary>
    Settings = 2
}

/// <summary>
/// The main view model for the application's primary window.
/// </summary>
public sealed class MainWindowViewModel : BaseViewModel
{
    private const double BaseWindowWidth = 840D; // see DesignTokens.xaml Window.BaseWidth
    private const double BaseWindowHeight = 560D; // see DesignTokens.xaml Window.BaseHeight

    // Base radius unit; multiplied by factor from settings to get real radius
    private const int CornerRadiusBase = 2;

    private readonly IFeatureServices _features;
    private readonly IShellServices _services;
    private readonly IShellViewModels _viewModels;

    private readonly ISettingsStore _settings;

    private bool _disposed;
    private bool _closeHandled;

    private string _dialogGridVisibility;
    private CornerRadius _itemTabRadiusLeft;
    private CornerRadius _itemTabRadiusRight;
    private AppTab _activeTab = AppTab.TaskLauncher;
    private AppTab? _lastActive;


    /// <summary>
    /// Gets the UI scale factor as a double (e.g., 1.0 for 100%, 1.25 for 125%).
    /// </summary>
    public double UiScale
    {
        get
        {
            return (double)_settings.Current.UiScalePercent / 100D;
        }
    }

    /// <summary>
    /// Gets the minimum window width based on the UI scale setting.
    /// </summary>
    public double WindowMinWidth
    {
        get
        {
            return ((double)_settings.Current.UiScalePercent / 100D) * BaseWindowWidth;
        }
    }

    /// <summary>
    /// Gets the minimum window height based on the UI scale setting.
    /// </summary>
    public double WindowMinHeight
    {
        get
        {
            return ((double)_settings.Current.UiScalePercent / 100D) * BaseWindowHeight;
        }
    }

    /// <summary>
    /// Gets or sets the visibility of the dialog grid overlay.
    /// </summary>
    public string DialogGridVisibility
    {
        get { return _dialogGridVisibility; }
        set { SetProperty(ref _dialogGridVisibility, value); }
    }

    /// <summary>
    /// Gets or sets the corner radius for the left side of item tabs.
    /// </summary>
    public CornerRadius ItemTabRadiusLeft
    {
        get { return _itemTabRadiusLeft; }
        set { SetProperty(ref _itemTabRadiusLeft, value); }
    }

    /// <summary>
    /// Gets or sets the corner radius for the right side of item tabs.
    /// </summary>
    public CornerRadius ItemTabRadiusRight
    {
        get { return _itemTabRadiusRight; }
        set { SetProperty(ref _itemTabRadiusRight, value); }
    }

    /// <summary>
    /// Gets or sets the currently active application tab.
    /// </summary>
    public AppTab ActiveTab
    {
        get { return _activeTab; }
        set
        {
            if (SetProperty(ref _activeTab, value))
            {
                NotifyTabChange(value);
            }
        }
    }


    /// <summary>
    /// Event raised when a request to close the main window is made.
    /// </summary>
    public event EventHandler? RequestClose;

    /// <summary>
    /// Event raised when a request to minimize the main window is made.
    /// </summary>
    public event EventHandler? RequestMinimize;


    /// <summary>
    /// Gets the command to close the main window.
    /// </summary>
    public ICommand CloseCommand { get; }

    /// <summary>
    /// Gets the command to minimize the main window.
    /// </summary>
    public ICommand MinimizeCommand { get; }

    /// <summary>
    /// Gets the command to open the help dialog.
    /// </summary>
    public ICommand HelpCommand { get; }


    /// <summary>
    /// Gets the Launcher tab view model.
    /// </summary>
    public LauncherTabViewModel Launcher { get; private set; }

    /// <summary>
    /// Gets the Add/Edit overlay view model.
    /// </summary>
    public LauncherDialogViewModel LauncherDialog { get; private set; }

    /// <summary>
    /// Gets the Killer tab view model.
    /// </summary>
    public KillerTabViewModel Killer { get; private set; }

    /// <summary>
    /// Gets the Settings tab view model.
    /// </summary>
    public SettingsTabViewModel Settings { get; private set; }


    /// <summary>
    /// Gets the modal dialog overlay view model.
    /// </summary>
    public ModalDialogViewModel ModalDialog { get; private set; }

    /// <summary>
    /// Gets the language picker dialog view model.
    /// </summary>
    public CultureDialogViewModel CultureDialog { get; private set; }

    /// <summary>
    /// Gets the about dialog view model.
    /// </summary>
    public AboutDialogViewModel AboutDialog { get; private set; }

    /// <summary>
    /// Gets the license dialog view model.
    /// </summary>
    public LicenseDialogViewModel LicenseDialog { get; private set; }

    /// <summary>
    /// Gets the help dialog view model.
    /// </summary>
    public HelpDialogViewModel HelpDialog { get; private set; }

    /// <summary>
    /// Gets the support dialog view model.
    /// </summary>
    public SupportDialogViewModel SupportDialog { get; private set; }


    /// <summary>
    /// Gets the adapter for monitoring running processes.
    /// </summary>
    public ProcessListAdapter Running { get; }

    /// <summary>
    /// Gets the settings store for application settings.
    /// </summary>
    public ISettingsStore SettingsStore
    {
        get { return _settings; }
    }

    /// <summary>
    /// Gets the tray notifications adapter.
    /// </summary>
    public ITrayNotifications Tray { get; private set; }

    /// <summary>
    /// Gets the feature services provider.
    /// </summary>
    public IFeatureServices Features
    {
        get { return _features; }
    }



#pragma warning disable CS8618 // To allow Design-time ctor branch
    /// <summary>
    /// Initializes a new instance of the <see cref="MainWindowViewModel"/> class for design-time use.
    /// </summary>
    public MainWindowViewModel()
    {
        if (DesignerProperties.GetIsInDesignMode(new DependencyObject()))
        {
            // Hide the dialog grid from Design view of MainWindow
            DialogGridVisibility = "Hidden";

            // Provide just enough shape for bindings, no services
            _settings = new SettingsStore();

            // Design-time VMs already avoid services
            LauncherDialog = null!;
            Launcher = new LauncherTabViewModel();
            Killer = new KillerTabViewModel();
            Settings = new SettingsTabViewModel();
        }
    }
#pragma warning restore CS8618


    /// <summary>
    /// Initializes a new instance of the <see cref="MainWindowViewModel"/> class.
    /// </summary>
    public MainWindowViewModel(
        IFeatureServices features,
        IShellServices services,
        IShellViewModels viewModels)
    {
        _features = features;
        _services = services;
        _viewModels = viewModels;
        _dialogGridVisibility = "Visible";

        Running = _services.Running;

        ModalDialog = _viewModels.Modal;

        HelpDialog = _viewModels.Help;
        SupportDialog = _viewModels.Support;
        LicenseDialog = _viewModels.License;
        AboutDialog = _viewModels.About;
        CultureDialog = _viewModels.Culture;

        Launcher = _viewModels.Launcher;
        LauncherDialog = _viewModels.LauncherDialog;
        Killer = _viewModels.Killer;
        Settings = _viewModels.Settings;


        _settings = _features.Settings;
        _settings.SettingsApplied += OnSettingsApplied;

        Tray = _features.Tray;

        CloseCommand = new RelayCommand(CloseMainWindow);
        MinimizeCommand = new RelayCommand(MinimizeMainWindow);
        HelpCommand = new RelayCommand(OpenHelp);

        ActiveTab = (AppTab)_features.Settings.Current.LastActiveTab;

        UpdateTabChrome();
    }



    /// <summary>
    /// Cleans up resources when the application is closing.
    /// </summary>
    public void OnAppClosing()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        _settings.SettingsApplied -= OnSettingsApplied;

        _ = HandleClose();
    }


    private void NotifyTabChange(AppTab now)
    {
        if (_lastActive is AppTab prev)
        {
            (GetVm(prev) as ITabAware)?.OnDeactivated();
        }

        (GetVm(now) as ITabAware)?.OnActivated();
        _lastActive = now;
    }

    private object? GetVm(AppTab tab)
    {
        return tab switch
        {
            AppTab.TaskLauncher => Launcher,
            AppTab.TaskKiller => Killer,
            AppTab.Settings => Settings,
            _ => null
        };
    }

    private void CloseMainWindow()
    {
        if (_settings.Current.MinimizeOnClose)
        {
            RequestMinimize?.Invoke(this, EventArgs.Empty);
            return;
        }

        _ = HandleClose(onCloseButtonAction: SendCloseRequest);
    }

    private void SendCloseRequest()
    {
        RequestClose?.Invoke(this, EventArgs.Empty);
    }

    private void MinimizeMainWindow()
    {
        RequestMinimize?.Invoke(this, EventArgs.Empty);
    }

    private void OpenHelp()
    {
        HelpDialog.Open();
    }

    private void UpdateTabChrome()
    {
        var cornerRadius = CornerRadiusBase * _settings.Current.CornerRadiusFactor;
        ItemTabRadiusLeft = new CornerRadius((double)cornerRadius, 0D, 0D, 0D);
        ItemTabRadiusRight = new CornerRadius(0D, (double)cornerRadius, 0D, 0D);
    }


    private void OnSettingsApplied(object? s, EventArgs e)
    {
        UpdateTabChrome();

        OnPropertiesChanged(nameof(UiScale), nameof(WindowMinWidth), nameof(WindowMinHeight));
    }


    private async Task HandleClose(Action? onCloseButtonAction = null)
    {
        if (_closeHandled)
        {
            return;
        }

        _closeHandled = true;

        // Allow views to handle on close tasks (i.e. unsaved changes modal dialog pop-up)
        await Settings.OnClosingAsync();
        await Killer.OnClosingAsync();
        await Launcher.OnClosingAsync();

        // Save window position 
        var win = System.Windows.Application.Current?.MainWindow;
        if (win != null && _settings.Current.RememberWindowPosition)
        {
            // If minimized/maximized, use RestoreBounds to get the last normal position
            var r = (win.WindowState == WindowState.Normal)
                ? new Rect(win.Left, win.Top, win.Width, win.Height)
                : win.RestoreBounds;

            _settings.Current.WindowPosX = (int)Math.Round(r.Left);
            _settings.Current.WindowPosY = (int)Math.Round(r.Top);
        }

        var isUninstall = string.IsNullOrEmpty(_settings.Current.AppDirectory);
        if (!isUninstall)
        {
            // Save settings on exit to remember last active tab and/or window position
            _settings.Apply(_settings.Current);
        }

        if (onCloseButtonAction == null)
        {
            return;
        }

        onCloseButtonAction();
    }
}
