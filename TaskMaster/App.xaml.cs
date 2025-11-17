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

using System.Diagnostics;
using System.Windows;

using TaskMaster.Application;
using TaskMaster.Application.Composition;
using TaskMaster.Infrastructure.Persistence;
using TaskMaster.Services;
using TaskMaster.Services.Logging;

namespace TaskMaster;

/// <summary>
/// TaskMaster is a simple application to keep desired tasks running<br/>
/// and prevent unwanted tasks from running on your computer.
/// <br/><br/>
/// <para>
/// <br>TaskMaster</br>
/// <br>Copyright (C) 2025 Aaron Semler</br>
/// <br><see href="https://github.com/SemlerPDX">github.com/SemlerPDX</see></br>
/// <br><see href="https://github.com/SemlerPDX/TaskMaster">TaskMaster on GitHub</see></br>
/// <br/><br/>
/// This program is free software: you can redistribute it and/or modify<br/>
/// it under the terms of the GNU General Public License as published by<br/>
/// the Free Software Foundation, either version 3 of the License, or<br/>
/// (at your option) any later version.<br/>
/// <br/>
/// This program is distributed in the hope that it will be useful,<br/>
/// but WITHOUT ANY WARRANTY; without even the implied warranty of<br/>
/// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the<br/>
/// GNU General Public License for more details.<br/>
/// <br/>
/// You should have received a copy of the GNU General Public License<br/>
/// along with this program.  If not, see <see href="https://www.gnu.org/licenses/">gnu.org/licenses</see>.
/// </para>
/// </summary>
public partial class App : System.Windows.Application
{
    // Named event is used to signal the existing instance to activate in enforced single instance
    private const string MutexName = "TaskMaster_by_SemlerPDX_{46536FDD-7842-4F6D-8216-4F29F7348B40}";
    private const string ActivateEventName = "TaskMaster_ActivateEvent_{46536FDD-7842-4F6D-8216-4F29F7348B40}";
    private const string ElevateFlag = "--from-self-elevate";

    private AppHost? _host;
    private Mutex? _singleInstanceMutex;
    private EventWaitHandle? _activateEvent;
    private RegisteredWaitHandle? _registeredWait;

    /// <summary>
    /// Application startup - enforce single instance and self-elevation if needed
    /// </summary>
    /// <param name="e">Startup event args</param>
    protected override void OnStartup(StartupEventArgs e)
    {
        try
        {
            HookGlobalExceptionLogging();
            
            AppUserModelId.Apply();

            _singleInstanceMutex = new Mutex(initiallyOwned: true, name: MutexName, createdNew: out bool createdNewMutex);

            // Must elevate early (if required) and bail out before single-instance wiring
            if (createdNewMutex && TrySelfElevateFromSetting(e.Args))
            {
                _singleInstanceMutex?.ReleaseMutex();
                _singleInstanceMutex?.Dispose();
                _singleInstanceMutex = null;
                return; // when app launched the elevated child and shut down
            }

            if (!createdNewMutex)
            {
                // Is second instance -> signal the primary to activate, then exit
                try
                {
                    // Try to open the named event created by the primary
                    EventWaitHandle.OpenExisting(ActivateEventName).Set();
                }
                catch
                {
                    // Fallback: could not open the event
                    System.Windows.MessageBox.Show(
                        "Another instance of Task Master is already running.",
                        "TaskMaster",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }

                Shutdown();
                return;
            }

            // Primary instance
            _activateEvent = new EventWaitHandle(false, EventResetMode.AutoReset, ActivateEventName, out bool createdNewEvent);

            // Callback is executed on a ThreadPool thread... must dispatch UI work
            _registeredWait = ThreadPool.RegisterWaitForSingleObject(
                _activateEvent,
                new WaitOrTimerCallback(HandleActivateEvent),
                null,
                Timeout.Infinite,
                executeOnlyOnce: false
            );

            // Spin up the TaskMaster app host:
            _host = new AppHost();
            _host.Start();
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "App Startup Fatal Error - see exception for details.");
            Log.Flush();
            Shutdown(-1);
        }
    }

    private void HandleActivateEvent(object? state, bool timedOut)
    {
        // Executes on a ThreadPool thread... marshal to UI
        Dispatcher.BeginInvoke(new Action(() =>
        {
            if (Current?.MainWindow is MainWindow mw)
            {
                mw.BringToFrontAndRestore();
            }
        }));
    }

    /// <summary>
    /// Cleanup event and registered wait on exit
    /// </summary>
    /// <param name="e">Exit event args</param>
    protected override void OnExit(ExitEventArgs e)
    {
        try
        {
            _host?.Dispose();
        }
        catch
        {
            // ...let it slide - best effort
        }

        try
        {
            Log.Flush();

            if (_registeredWait != null && _activateEvent != null)
            {
                _registeredWait.Unregister(_activateEvent);
                _registeredWait = null;
            }

            _activateEvent?.Close();
            _activateEvent = null;
        }
        catch
        {
            // ...let it slide
        }

        try
        {
            _singleInstanceMutex?.ReleaseMutex();
            _singleInstanceMutex?.Dispose();
        }
        catch
        {
            // ...let it slide
        }

        base.OnExit(e);
    }

    private void HookGlobalExceptionLogging()
    {
        AppDomain.CurrentDomain.UnhandledException += (s, e) =>
        {
            try
            {
                var ex = e.ExceptionObject as Exception ?? new Exception(e.ExceptionObject?.ToString() ?? "Unknown fatal error");
                Log.Fatal(ex, "AppDomain.CurrentDomain.UnhandledException" + $" Non-UI thread exception (IsTerminating={e.IsTerminating})");
                Log.Flush();
            }
            catch
            {
                // ...let it slide
            }
        };

        this.DispatcherUnhandledException += (s, e) =>
        {
            try
            {
                Log.Fatal(e.Exception, "Application.DispatcherUnhandledException UI thread exception");
                Log.Flush();
            }
            catch
            {
                // ...let it slide
            }
            finally
            {
                e.Handled = true;
                Shutdown(-1);
            }
        };

        TaskScheduler.UnobservedTaskException += (s, e) =>
        {
            try
            {
                Log.Fatal(e.Exception, "TaskScheduler.UnobservedTaskException");
                Log.Flush();
            }
            catch
            {
                // ...let it slide
            }
            finally
            {
                e.SetObserved();
            }
        };
    }

    private bool TrySelfElevateFromSetting(string[] args)
    {
        // Avoid infinite loops if already the elevated child
        if (args != null && args.Contains(ElevateFlag, StringComparer.OrdinalIgnoreCase))
        {
            return false;
        }

        if (AppInfo.IsElevated)
        {
            return false;
        }

        // Load temporary settings and check RunAsAdmin
        var tempSettings = new SettingsStore();
        if (!tempSettings.Current.RunAsAdmin)
        {
            return false;
        }

        try
        {
            // If the elevated scheduled task exists, try to run it to avoid annoying UAC prompt
            if (!ScheduledTaskHandler.TryRunTask(AppInfo.Name))
            {
                // ...else Build the elevated child start info
                string exe = AppInfo.Location;
                if (string.IsNullOrEmpty(exe))
                {
                    Log.Error("Unable to self-elevate: application path is null or empty.");
                    return false; // can't elevate without a path
                }

                var argList = (args ?? []).ToList();
                argList.Add(ElevateFlag);

                var psi = new ProcessStartInfo(exe)
                {
                    UseShellExecute = true,
                    Verb = "runas",
                    Arguments = string.Join(" ", argList)
                };

                // Normal start elevated (trigger UAC prompt)
                Process.Start(psi);
            }

            // Exit this (non-elevated) instance before creating any UI or mutex
            Shutdown();
            return true;
        }
        catch
        {
            // User canceled UAC or elevation failed... fall back to normal startup
            return false;
        }
    }
}
