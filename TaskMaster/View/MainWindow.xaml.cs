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

using System.IO;
using System.Windows;
using System.Windows.Input;

using TaskMaster.Application;
using TaskMaster.Infrastructure.Persistence;
using TaskMaster.Presentation.UI;
using TaskMaster.Services.Logging;
using TaskMaster.ViewModel;

namespace TaskMaster;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private const string AppByLine = "TaskMaster by SemlerPDX";

    private ISettingsStore? _settingsStore;

    private MainWindowViewModel? _wiredVm; // keep reference to prevent GC
    private Icon? _appIcon; // keep alive for tray
    private NotifyIcon? _notifyIcon;

    private bool _startupMinHandled = false;
    private bool _minimizeToTray = false;
    private bool _isNotifyIconSet = false;
    private bool _isFirstTimeUse = false;


    /// <summary>
    /// Initializes a new instance of the <see cref="MainWindow"/> class.
    /// </summary>
    public MainWindow()
    {
        this.Opacity = 0;
        InitializeComponent();

        // Covers XAML-set DataContext during InitializeComponent
        RewireVm(null, DataContext as MainWindowViewModel);

        // Handle future swaps (if any)
        DataContextChanged += (_, e) =>
            RewireVm(e.OldValue as MainWindowViewModel, e.NewValue as MainWindowViewModel);

        // Detach if/when view unloads
        Unloaded += (_, __) => RewireVm(_wiredVm, null);

        Loaded += (_, __) =>
        {
            if (DataContext is not MainWindowViewModel vm)
            {
                return;
            }

            _settingsStore = vm.SettingsStore;
            HandleAppliedSettings(_settingsStore);

            vm.Features.Visibility.Attach(this);

            // Apply startup settings
            if (_isFirstTimeUse)
            {
                // Must run after layout/render so sizes and RestoreBounds are valid
                Dispatcher.BeginInvoke(new Action(CenterOnPrimaryWorkingArea),
                    System.Windows.Threading.DispatcherPriority.ApplicationIdle);
            }
            else if (_settingsStore.Current.RememberWindowPosition)
            {
                MoveWindowPos(_settingsStore.Current.WindowPosX, _settingsStore.Current.WindowPosY);
            }

            // React to future settings commits
            _settingsStore.SettingsApplied += (_, __) =>
            {
                HandleAppliedSettings(_settingsStore);
            };

            AppDialog.Current = new InAppDialogService(vm.ModalDialog);

            if (!_settingsStore.Current.StartMinimized)
            {
                this.Opacity = 1;
                this.Activate();
                this.Topmost = true;
                this.Topmost = false;
                this.Focus();
                this.ShowInTaskbar = true;
            }

            ApplyAlwaysOnTop(_settingsStore.Current.AlwaysOnTop);
            ApplyBrandingAndTrayIcon();
        };

    }


    private void OnVmRequestClose(object? sender, EventArgs e) => Close();
    private void OnVmRequestMinimize(object? sender, EventArgs e) => MinimizeApp();

    /// <summary>
    /// Handle minimize to tray behavior.<br/>
    /// </summary>
    /// <param name="e">Event args.</param>
    protected override void OnStateChanged(EventArgs e)
    {
        base.OnStateChanged(e);
        if (!_minimizeToTray)
        {
            return;
        }

        // Auto-minimize to tray when minimized
        if (WindowState == WindowState.Minimized)
        {
            HideToTray();
        }
    }

    /// <summary>
    /// Handle startup minimize after content is rendered to ensure correct bounds.<br/>
    /// </summary>
    /// <param name="e">Event args.</param>
    protected override void OnContentRendered(EventArgs e)
    {
        base.OnContentRendered(e);

        if (_startupMinHandled)
        {
            return;
        }

        _startupMinHandled = true;

        var startMinimized = _settingsStore?.Current.StartMinimized ?? false;

        if (startMinimized)
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                // The content is fully rendered; bounds are correct here
                this.WindowState = WindowState.Minimized;
                ShowInTaskbar = !_settingsStore?.Current.MinimizeToTray ?? true;
                this.Opacity = 1;
            }), System.Windows.Threading.DispatcherPriority.ApplicationIdle);
        }
        else
        {
            if (_notifyIcon != null)
            {
                _notifyIcon.Visible = false;
            }
        }
    }

    /// <summary>
    /// Bring the main window to the foreground, restoring if minimized or hidden, and center on main monitor.<br/>
    /// Used when a second instance signals: launching second instance merely restores/shows the first prominently.
    /// </summary>
    public void BringToFrontAndRestore()
    {
        try
        {
            Dispatcher.BeginInvoke(new Action(CenterOnPrimaryWorkingArea));
        }
        catch
        {
            // let it slide... UI may not be ready, etc.
        }
    }

    /// <summary>
    /// Attach the view-owned NotifyIcon to the specified tray notification service.
    /// </summary>
    /// <param name="tray">The tray notification service to attach to.</param>
    public void AttachTray(ITrayNotifications tray)
    {
        if (_notifyIcon == null)
        {
            InitializeTrayIcon(ExtractExeIcon());
        }

        tray.Attach(_notifyIcon!);
        _isNotifyIconSet = true;

    }


    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        try
        {
            if (e.ButtonState == MouseButtonState.Pressed)
            {
                DragMove();
            }
        }
        catch
        {
            // ...let it slide
        }
    }

    private void RewireVm(MainWindowViewModel? oldVm, MainWindowViewModel? newVm)
    {
        if (oldVm != null)
        {
            WeakEventManager<MainWindowViewModel, EventArgs>
                .RemoveHandler(oldVm, nameof(MainWindowViewModel.RequestClose), OnVmRequestClose);

            WeakEventManager<MainWindowViewModel, EventArgs>
                .RemoveHandler(oldVm, nameof(MainWindowViewModel.RequestMinimize), OnVmRequestMinimize);
        }

        _wiredVm = newVm;

        if (newVm != null)
        {
            WeakEventManager<MainWindowViewModel, EventArgs>
                .AddHandler(newVm, nameof(MainWindowViewModel.RequestClose), OnVmRequestClose);

            WeakEventManager<MainWindowViewModel, EventArgs>
                .AddHandler(newVm, nameof(MainWindowViewModel.RequestMinimize), OnVmRequestMinimize);
        }
    }

    private void HandleAppliedSettings(ISettingsStore settingsService)
    {
        _isFirstTimeUse = _settingsStore?.IsFirstTimeSetup ?? false;

        _minimizeToTray = settingsService.Current.MinimizeToTray;

        DesignTokenUpdater.SetToolTipTimings(settingsService.Current.ToolTipDelay, settingsService.Current.ToolTipDuration);
        Dispatcher.Invoke(ApplyAlwaysOnTop);
    }

    private void CenterOnPrimaryWorkingArea()
    {
        try
        {
            // Ensure window is visible and not minimized first
            RestoreFromTray();

            // Primary monitor working area
            var wa = SystemParameters.WorkArea;
            double workLeft = wa.Left;
            double workTop = wa.Top;
            double workWidth = wa.Width;
            double workHeight = wa.Height;

            // Pick the correct window size in DIPs
            double w = (WindowState == WindowState.Normal) ? Width : RestoreBounds.Width;
            double h = (WindowState == WindowState.Normal) ? Height : RestoreBounds.Height;

            // If Width/Height are not set (NaN) or 0, force a layout pass and use Actual
            if (double.IsNaN(w) || w <= 0)
            {
                UpdateLayout();
                w = ActualWidth;
            }

            if (double.IsNaN(h) || h <= 0)
            {
                UpdateLayout();
                h = ActualHeight;
            }

            Left = workLeft + (workWidth - w) / 2.0;
            Top = workTop + (workHeight - h) / 2.0;
        }
        catch (Exception ex)
        {
            Log.Error(ex, $"{Uid}: Failed to center main window on primary working area.");
        }
    }

    private void MinimizeApp()
    {
        if (_minimizeToTray)
        {
            this.ShowInTaskbar = false;
            this.Visibility = Visibility.Collapsed;
        }

        WindowState = WindowState.Minimized;
    }

    private void HideToTray()
    {
        if (!_minimizeToTray)
        {
            return;
        }

        // Show tray icon and hide window/taskbar entry
        if (_notifyIcon != null)
        {
            _notifyIcon.Visible = true;
        }

        this.ShowInTaskbar = false;
        this.Hide();
    }

    private void RestoreFromTray()
    {
        if (_notifyIcon != null)
        {
            _notifyIcon.Visible = false;
        }

        if (this.Visibility != Visibility.Visible)
        {
            this.Show();
        }

        if (!this.ShowInTaskbar)
        {
            this.ShowInTaskbar = true;
        }

        if (this.WindowState == WindowState.Minimized)
        {
            this.WindowState = WindowState.Normal;
        }

        this.Opacity = 1;
        this.Activate();
        this.Topmost = true;
        this.Topmost = false;
        this.Focus();

        ApplyAlwaysOnTop();
    }

    private void MoveWindowPos(double x, double y)
    {
        this.Left = x;
        this.Top = y;
    }

    private void ApplyAlwaysOnTop() => ApplyAlwaysOnTop(null);
    private void ApplyAlwaysOnTop(bool? onTop = null)
    {
        this.Topmost = true;

        var setOnTop = _settingsStore?.Current.AlwaysOnTop ?? false;
        if (onTop != null)
        {
            setOnTop = onTop ?? false;
        }

        if (this.Topmost != setOnTop)
        {
            this.Topmost = setOnTop;
        }
    }

    private void ApplyBrandingAndTrayIcon()
    {
        // Titlebar name + version suffix (if not release)
        SetBetaVersionFromAssembly();

        // Tray + shell icon from EXE .ico
        _appIcon = ExtractExeIcon();
    }

    private void SetBetaVersionFromAssembly()
    {
        var ver = AppInfo.VersionString;
        bool isRelease = ver.Split('.', StringSplitOptions.RemoveEmptyEntries).Length <= 3;
        var suffix = isRelease ? string.Empty : "   v" + ver;

        TitleBarSuffix.Text = suffix;
    }

    private void InitializeTrayIcon(Icon? icon)
    {
        if (_isNotifyIconSet)
        {
            return;
        }

        _isNotifyIconSet = true;

        _notifyIcon = new NotifyIcon
        {
            Icon = icon ?? SystemIcons.Application,
            Visible = false,
            Text = AppByLine
        };

        _notifyIcon.DoubleClick += (s, e) =>
        {
            Dispatcher.BeginInvoke(new Action(RestoreFromTray));
        };

        // Attach View-owned NotifyIcon to tray service
        if (DataContext is MainWindowViewModel vm && _notifyIcon != null)
        {
            vm.Features.Tray.Attach(_notifyIcon);
        }
    }

    private static Icon? ExtractExeIcon()
    {
        var exePath = AppInfo.Location;
        if (string.IsNullOrEmpty(exePath) || !File.Exists(exePath))
        {
            return null;
        }

        try
        {
            return System.Drawing.Icon.ExtractAssociatedIcon(exePath);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to extract application icon from EXE.");
            return null;
        }
    }


    /// <summary>
    /// Final cleanup when the Window is actually closed.<br/>
    /// </summary>
    /// <param name="e">Event args.</param>
    protected override void OnClosed(EventArgs e)
    {
        // Final cleanup when the Window is actually closed
        if (DataContext is MainWindowViewModel vm)
        {
            vm.OnAppClosing();
            vm.Tray.Detach();
            vm.Features.Visibility.Detach();
        }

        // Dispose icon before exiting to prevent ghost icons
        if (_notifyIcon != null)
        {
            _notifyIcon.Visible = false;
            _notifyIcon.Dispose();
            _notifyIcon = null;
        }

        _appIcon?.Dispose();
        _appIcon = null;

        base.OnClosed(e);
    }
}
