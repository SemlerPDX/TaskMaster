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
using System.Runtime.InteropServices;
using System.Windows.Interop;
using System.Windows.Threading;

using TaskMaster.Application.Features;
using TaskMaster.Domain.Policies;
using TaskMaster.Infrastructure.Persistence;
using TaskMaster.Infrastructure.Processes;
using TaskMaster.Presentation.Localization;
using TaskMaster.Presentation.UI;
using TaskMaster.Services;
using TaskMaster.Services.Logging;
using TaskMaster.ViewModel;
using TaskMaster.ViewModel.Dialog;

namespace TaskMaster.Application.Composition;

/// <summary>
/// Builds the object graph, shows MainWindow, and owns lifetimes.
/// </summary>
public sealed class AppHost : IDisposable
{
    private const string UpdateFoundMessageKey = "Message.App.History.UpdateFound";
    private const string UpdateNotFoundMessageKey = "Message.App.History.UpdateNotFound";
    private const string UpdateErrorMessageKey = "Message.App.History.UpdateError";

    private const string UpdateFoundTrayTitleKey = "Title.App.Tray.UpdateFound";
    private const string UpdateFoundTrayMessageKey = "Message.App.Tray.UpdateFound";

    private const int DefaultUpdateCheckStartupMs = 5000;  // milliseconds
    private const int DefaultFeatureStartupDelayMs = 3000; // milliseconds
    private const int WM_SETTINGCHANGE = 0x001A;

    // Core services
    private readonly SettingsStore _settings;
    private readonly ConfigStore _config;
    private readonly ConfigFileHandler _configFileHandler;
    private readonly LocalizationService _localization;
    private readonly ThemeService _theme;
    private readonly StyleService _style;
    private readonly ProcessMonitorService _monitor;

    // Process policies
    private readonly IProcessExclusionsStore _exclusionsStore;
    private readonly IProcessExclusionPolicy _exclusions;
    private readonly IUniqueEntryPolicy _unique;

    // Presentation-adjacent services
    private readonly ITrayNotifications _tray;
    private readonly IAppVisibility _appVisibility;
    private readonly HistoryHub _history;
    private readonly ProcessListAdapter _running;

    // Common / Feature services
    private readonly ICommonServices _common;
    private readonly IFeatureServices _features;
    private readonly IShellServices _shell;
    private readonly TaskKiller _taskKiller;
    private readonly TaskLauncher _taskLauncher;

    // Child VMs
    private readonly ModalDialogViewModel _modal;
    private readonly HelpDialogViewModel _help;
    private readonly SupportDialogViewModel _support;
    private readonly LicenseDialogViewModel _license;
    private readonly AboutDialogViewModel _about;
    private readonly CultureDialogViewModel _culture;
    private readonly LauncherDialogViewModel _launcherDialog;
    private readonly LauncherTabViewModel _launcherVm;
    private readonly KillerTabViewModel _killerVm;
    private readonly SettingsTabViewModel _settingsVm;

    // Window handle for broadcasting WM_SETTINGCHANGE (Auto-Theme "Dark" or "Light")
    private HwndSource? _hwnd;


    /// <summary>
    /// Gets the MainWindow instance.
    /// </summary>
    public MainWindow? Window { get; private set; }

    /// <summary>
    /// Gets the root MainWindow view model.
    /// </summary>
    public MainWindowViewModel? RootVm { get; private set; }


    /// <summary>
    /// Creates a new application host, building the object graph.
    /// </summary>
    public AppHost()
    {
        _settings = new SettingsStore();

        Log.ReconfigureFrom(_settings.Current);

        _localization = new LocalizationService();
        _theme = new ThemeService();
        _style = new StyleService();

        _configFileHandler = new ConfigFileHandler();
        _config = new ConfigStore(_configFileHandler);
        _ = _config.LoadOrNew();

        var seconds = (double)_settings.Current.ProcessPollingIntervalSeconds;
        _monitor = new ProcessMonitorService(seconds);

        _exclusionsStore = new ProcessExclusionsStore();
        _exclusions = new ProcessExclusionPolicy(_exclusionsStore);
        _unique = new UniqueEntryPolicy();

        _history = new HistoryHub();
        _appVisibility = new AppVisibilityService();
        _tray = new TrayNotificationService(_settings, _appVisibility);
        _running = new ProcessListAdapter(_monitor, _exclusions, _appVisibility);

        _common = new CommonServices(_localization, _settings, _config, _tray, _appVisibility);
        _features = new FeatureServices(_common, _history.Launcher, _history.Killer);

        _taskKiller = new TaskKiller(_monitor, _features);
        _taskLauncher = new TaskLauncher(_monitor, _features);

        _shell = new ShellServices(
            _theme, _style, _monitor, _exclusionsStore, _exclusions, _unique, _running,
            _taskKiller, _taskLauncher);

        // All the Child VMs
        _modal = new ModalDialogViewModel { IsOpen = false };
        _license = new LicenseDialogViewModel();
        _about = new AboutDialogViewModel(_license, _features);
        _help = new HelpDialogViewModel(_features);
        _support = new SupportDialogViewModel(_features);
        _culture = new CultureDialogViewModel(_about, _features);

        _launcherDialog = new LauncherDialogViewModel(_features, _unique);
        _launcherVm = new LauncherTabViewModel(_launcherDialog, _features, _unique);
        _killerVm = new KillerTabViewModel(_features, _unique);
        _settingsVm = new SettingsTabViewModel(_about, _license, _support, _features, _shell);
    }


    /// <summary>
    /// Starts the application host, showing MainWindow and initializing features.
    /// </summary>
    public void Start()
    {
        // Applying visual/bootstrap policies affecting the whole app first
        _theme.ApplyTheme(ResolveEffectiveTheme(_settings.Current.Theme));
        _style.ApplyStyle(_settings.Current.CornerRadiusFactor);
        _localization.ApplyCulture(_settings.Current.UiCulture);

        var vms = new ShellViewModels(
            _launcherVm, _killerVm, _settingsVm,
            _launcherDialog, _modal, _culture, _about, _license, _help, _support);

        // MainWindow is root VM in TaskMaster
        RootVm = new MainWindowViewModel(_features, _shell, vms);
        Window = new MainWindow { DataContext = RootVm };
        System.Windows.Application.Current.MainWindow = Window;
        Window.Show();

        // Applying Jump List (taskbar right-click menu) with localized titles
        JumpListService.ApplyDefaultJumpList(_localization);

        var helper = new WindowInteropHelper(Window);
        _hwnd = HwndSource.FromHwnd(helper.Handle);
        _hwnd?.AddHook(WndProc);

        if (RootVm?.Tray != null)
        {
            Window.AttachTray(RootVm.Tray);
        }

        // First-time setup flow - show language picker, then About page with disclaimer + GNU license link
        if (_settings.IsFirstTimeSetup)
        {
            System.Windows.Application.Current.Dispatcher.BeginInvoke(new Action(() => _culture.Open()));
        }

        // Starting features only after UI is idle
        var delay = TimeSpan.FromMilliseconds(DefaultFeatureStartupDelayMs);
        if (_settings.Current.EnableProcessKiller)
        {
            System.Windows.Application.Current.Dispatcher.BeginInvoke(
                new Action(() => _taskKiller.Start(delay)),
                DispatcherPriority.ContextIdle);
        }

        if (_settings.Current.EnableProcessLauncher)
        {
            System.Windows.Application.Current.Dispatcher.BeginInvoke(
                new Action(() => _taskLauncher.Start(delay)),
                DispatcherPriority.ContextIdle);
        }

        if (_settings.Current.CheckForUpdates)
        {
            CheckUpdatesOnStartup(DefaultUpdateCheckStartupMs);
        }

        var exePath = AppInfo.Location ?? Path.Combine(_settings.Current.AppDirectory, AppInfo.Name + ".exe");
        StartupSettingsHandler.Apply(exePath, AppInfo.Name, AppInfo.IsElevated, _settings.Current.StartWithWindows);

        _settings.SettingsApplied += OnSettingsApplied;
    }


    private void CheckUpdatesOnStartup(int delayMs)
    {
        _ = Task.Run(async () =>
        {
            try
            {
                Thread.Sleep(delayMs); // slight delay to avoid clashing with other startup tasks

                var updateData = await UpdateChecker.CheckForUpdatesAsync(_settings.Current.UiCulture);
                if (updateData == null)
                {
                    LogHistory(UpdateErrorMessageKey);
                }
                else if (updateData.Length == 1)
                {
                    LogHistory(UpdateNotFoundMessageKey, [updateData[0]]);
                }
                else
                {
                    LogTray(UpdateFoundTrayTitleKey, UpdateFoundTrayMessageKey, [updateData[0], updateData[1]]);
                    LogHistory(UpdateFoundMessageKey, [updateData[0]]);
                }
            }
            catch // any logging performed at the UpdateChecker level
            {
                LogHistory(UpdateErrorMessageKey);
            }
        });
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == WM_SETTINGCHANGE)
        {
            string? param = lParam != IntPtr.Zero ? Marshal.PtrToStringUni(lParam) : null;

            // Windows sends these when the app theme preference changes
            if (string.Equals(param, "ImmersiveColorSet", StringComparison.Ordinal) ||
                string.Equals(param, "AppsUseLightTheme", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(param, "SystemUsesLightTheme", StringComparison.OrdinalIgnoreCase))
            {
                OnOsThemeMaybeChanged();
            }
        }

        return IntPtr.Zero;
    }

    private string ResolveEffectiveTheme(string theme)
    {
        if (string.Equals(theme, ThemeCatalog.AutoTheme, StringComparison.OrdinalIgnoreCase))
        {
            return _theme.IsOsLightTheme() ? ThemeCatalog.LightTheme : ThemeCatalog.DarkTheme;
        }

        return theme;
    }

    private void LogHistory(string key) => LogHistory(key, []);
    private void LogHistory(string key, params object[] args)
    {
        InfoService.LogLauncherHistory(_features, key, null, args);
        InfoService.LogKillerHistory(_features, key, null, args);
    }

    private void LogTray(string titleKey, string messageKey, params object[] args)
    {
        var trayData = new InfoService.TrayData
        {
            TitleKey = titleKey,
            MessageKey = messageKey
        };

        InfoService.TrayNotification(_features, trayData, args);
    }


    private void OnOsThemeMaybeChanged()
    {
        if (!string.Equals(_settings.Current.Theme, ThemeCatalog.AutoTheme, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var effective = _theme.IsOsLightTheme() ? ThemeCatalog.LightTheme : ThemeCatalog.DarkTheme;
        _theme.ApplyTheme(effective);
    }

    private void OnSettingsApplied(object? s, EventArgs e)
    {
        Log.ReconfigureFrom(_settings.Current);

        var seconds = (double)_settings.Current.ProcessPollingIntervalSeconds;
        _monitor.UpdateInterval(seconds);

        _theme.ApplyTheme(ResolveEffectiveTheme(_settings.Current.Theme));
        _localization.ApplyCulture(_settings.Current.UiCulture);
    }


    /// <summary>
    /// Disposes the application host and its resources.
    /// </summary>
    public void Dispose()
    {
        if (_hwnd != null)
        {
            _hwnd.RemoveHook(WndProc);
            _hwnd = null;
        }

        try
        {
            // Lets the VM save window state/unsubscribe, etc.
            RootVm?.OnAppClosing();
        }
        catch
        {
            // ...let it slide - best effort
        }

        _settings.SettingsApplied -= OnSettingsApplied;

        // Infra disposal in reverse creation order (where sensible)
        (_taskLauncher as IDisposable)?.Dispose();
        (_taskKiller as IDisposable)?.Dispose();
        (_running as IDisposable)?.Dispose();
        (_exclusions as IDisposable)?.Dispose();
        (_monitor as IDisposable)?.Dispose();

        (_tray as IDisposable)?.Dispose();
        (_appVisibility as IDisposable)?.Dispose();
        (_history as IDisposable)?.Dispose();
    }
}
