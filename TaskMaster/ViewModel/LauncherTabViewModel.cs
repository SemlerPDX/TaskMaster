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

using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Input;

using TaskMaster.Application.Composition;
using TaskMaster.Domain.Policies;
using TaskMaster.Infrastructure.Processes;
using TaskMaster.Model;
using TaskMaster.Presentation.Commands;
using TaskMaster.Presentation.UI;
using TaskMaster.Services;

namespace TaskMaster.ViewModel;

/// <summary>
/// The view model for the application's Task Launcher tab.
/// </summary>
public sealed class LauncherTabViewModel : BaseViewModel, ITabAware
{
    private const string ChangedTitleKey = "Title.LauncherTab.Dialog.Changed";
    private const string ChangedMessageKey = "Message.LauncherTab.Dialog.Changed";

    private const string BrowseFileTitleKey = "Title.BrowseFile";

    private const string SavedMessageKey = "Message.LauncherTab.History.Saved";
    private const string CanceledMessageKey = "Message.LauncherTab.History.Canceled";
    private const string UnableMessageKey = "Message.LauncherTab.History.Unable";
    private const string PathErrorMessageKey = "Message.LauncherTab.History.PathError";

    private const string MissingProcessName = "[[MISSING NAME]]";


    private readonly LauncherDialogViewModel _dialog;
    private readonly IFeatureServices _features;
    private readonly IUniqueEntryPolicy _uniqueEntryPolicy;


    private LauncherRowViewModel? _selectedLauncher;

    private bool _isActive = false;
    private bool _squelch; // prevents recursive setter ping-pong

    private string? _selectedRunning;
    private bool _saveButtonEnabled;

    private readonly RelayCommand _saveCommand;
    private readonly RelayCommand _cancelCommand;
    private readonly RelayCommand _clearHistory;
    private readonly RelayCommand _addFromRunningCommand;
    private readonly RelayCommand _moveUpCommand;
    private readonly RelayCommand _moveDownCommand;
    private readonly RelayCommand _editSavedCommand;
    private readonly RelayCommand _removeSavedCommand;


    /// <summary>
    /// A view model representing a single row in the saved launchers list.
    /// </summary>
    public sealed class LauncherRowViewModel : BaseViewModel
    {
        /// <summary>
        /// The underlying model data for this launcher entry.
        /// </summary>
        public LauncherData Model { get; }

        /// <summary>
        /// A user-friendly display name for the launcher, derived from the entry's name.
        /// </summary>
        public string DisplayName
        {
            get
            {
                var name = Model.Entry?.Name ?? string.Empty;
                return string.IsNullOrWhiteSpace(name) ? MissingProcessName : name;
            }
        }

        /// <summary>
        /// Construct a new LauncherRowViewModel wrapping the provided model.
        /// </summary>
        /// <param name="model">The LauncherData model to wrap.</param>
        /// <exception cref="ArgumentNullException">Thrown if model is null.</exception>
        public LauncherRowViewModel(LauncherData model)
        {
            Model = model ?? throw new ArgumentNullException(nameof(model));
        }
    }


    /// <summary>
    /// The collection of saved task launcher processes for display and selection.<br/>
    /// A "Ghost" working list bound to the left listbox; changes persisted only upon save.
    /// </summary>
    public ObservableCollection<LauncherRowViewModel> WorkingLaunchers { get; } = [];

    /// <summary>
    /// A read-only collection of history log entries for the Task Launcher tab.
    /// </summary>
    public ReadOnlyObservableCollection<string> LauncherHistory
    {
        get
        {
            if (_features == null)
            {
                return new ReadOnlyObservableCollection<string>([]);
            }

            return _features.LauncherHistory.Items;
        }
    }


    /// <summary>
    /// The currently selected launcher entry in the saved list.
    /// </summary>
    public LauncherRowViewModel? SelectedLauncher
    {
        get { return _selectedLauncher; }
        set
        {
            if (SetProperty(ref _selectedLauncher, value))
            {
                if (!_squelch && value != null)
                {
                    _squelch = true;
                    SelectedRunning = null; // makes the other Selection list "not active"
                    _squelch = false;
                }

                RaiseRowActionCanExecutes();
            }
        }
    }

    /// <summary>
    /// The currently selected running process name from the running processes list.
    /// </summary>
    public string? SelectedRunning
    {
        get { return _selectedRunning; }
        set
        {
            if (SetProperty(ref _selectedRunning, value))
            {
                if (!_squelch && !string.IsNullOrWhiteSpace(value))
                {
                    _squelch = true;
                    SelectedLauncher = null; // makes the other Selection list "not active"
                    _squelch = false;
                }

                RaiseRowActionCanExecutes();
            }
        }
    }

    /// <summary>
    /// Indicates whether the Save button is enabled (there are unsaved changes).
    /// </summary>
    public bool SaveButtonEnabled
    {
        get { return _saveButtonEnabled; }
        private set
        {
            if (SetProperty(ref _saveButtonEnabled, value))
            {
                // Save/Undo buttons
                _saveCommand?.RaiseCanExecuteChanged();
                _cancelCommand?.RaiseCanExecuteChanged();

                // Inverse-enabled: block dialog entry points while dirty
                RaiseRowActionCanExecutes();
            }
        }
    }

    /// <summary>
    /// Command to save changes to the launcher list.
    /// </summary>
    public ICommand SaveCommand => _saveCommand;

    /// <summary>
    /// Command to cancel unsaved changes to the launcher list.
    /// </summary>
    public ICommand CancelCommand => _cancelCommand;

    /// <summary>
    /// Command to clear the launcher history log.
    /// </summary>
    public ICommand ClearHistory => _clearHistory;


    /// <summary>
    /// Command to add a new launcher entry from the selected running process.
    /// </summary>
    public ICommand AddFromRunningCommand => _addFromRunningCommand;

    /// <summary>
    /// Command to open a file dialog to browse for an application to add as a launcher.
    /// </summary>
    public ICommand BrowseMainCommand { get; }

    /// <summary>
    /// Command to move the selected launcher entry up in the list.
    /// </summary>
    public ICommand MoveUpCommand => _moveUpCommand;

    /// <summary>
    /// Command to move the selected launcher entry down in the list.
    /// </summary>
    public ICommand MoveDownCommand => _moveDownCommand;

    /// <summary>
    /// Command to edit the currently selected saved launcher entry.
    /// </summary>
    public ICommand EditSavedCommand => _editSavedCommand;

    /// <summary>
    /// Command to remove the currently selected saved launcher entry.
    /// </summary>
    public ICommand RemoveSavedCommand => _removeSavedCommand;



#pragma warning disable CS8618 // To allow Design-time ctor branch
    /// <summary>
    /// Initializes a new instance of the <see cref="LauncherTabViewModel"/> class for design-time use.
    /// </summary>
    public LauncherTabViewModel()
    {
        if (DesignerProperties.GetIsInDesignMode(new DependencyObject()))
        {
            SaveButtonEnabled = true;
        }
    }
#pragma warning restore CS8618

    /// <summary>
    /// Initializes a new instance of the <see cref="LauncherTabViewModel"/> class.
    /// </summary>
    /// <param name="dialog">The launcher dialog view model for add/edit operations.</param>
    /// <param name="services">The feature services for configuration and history, etc.</param>
    /// <param name="uniqueEntryPolicy">The policy to enforce unique launcher entries.</param>
    public LauncherTabViewModel(
        LauncherDialogViewModel dialog,
        IFeatureServices services,
        IUniqueEntryPolicy uniqueEntryPolicy)
    {
        _features = services;
        _dialog = dialog;
        _uniqueEntryPolicy = uniqueEntryPolicy;

        RebuildWorkingList();

        _features.Config.SaveDataUpdated += (_, __) => RebuildWorkingList();

        _saveCommand = new RelayCommand(Save, () => SaveButtonEnabled);
        _cancelCommand = new RelayCommand(Cancel, () => SaveButtonEnabled);

        _addFromRunningCommand = new RelayCommand(AddFromRunning, () => !string.IsNullOrWhiteSpace(SelectedRunning));
        BrowseMainCommand = new RelayCommand(BrowseMain);

        _moveUpCommand = new RelayCommand(() => MoveSelectedApp(-1), () => CanMoveSelected(-1));
        _moveDownCommand = new RelayCommand(() => MoveSelectedApp(1), () => CanMoveSelected(1));
        _editSavedCommand = new RelayCommand(() => _ = EditSaved(), () => SelectedLauncher != null);
        _removeSavedCommand = new RelayCommand(RemoveSaved, () => SelectedLauncher != null);

        _clearHistory = new RelayCommand(
            () => _features.LauncherHistory.Clear(),
            () => _features.LauncherHistory.Items.Count > 0);

        if (_features.LauncherHistory.Items is INotifyCollectionChanged incc)
        {
            incc.CollectionChanged += OnHistoryChanged;
        }
    }



    /// <summary>
    /// Event raised to request the view to scroll to the bottom of the history log.
    /// </summary>
    public event EventHandler? RequestScrollToBottom;

    void ITabAware.OnActivated()
    {
        _squelch = true;
        SelectedRunning = null;
        SelectedLauncher = null;
        _squelch = false;
        Cancel();

        RequestScrollToBottom?.Invoke(this, EventArgs.Empty);

        _features.Settings.Current.LastActiveTab = (int)AppTab.TaskLauncher;

        _isActive = true;
    }

    void ITabAware.OnDeactivated()
    {
        _isActive = false;

        OnViewChanged();
    }


    private void Save()
    {
        // Apply the ghost list to persisted store in one shot (preserve Id + Entry order)
        _features.Config.Current.Launchers = WorkingLaunchers
            .Select(l => l.Model)
            .ToList();

        _features.Config.Save(_features.Config.Current);

        // Clear dirty (starts with a synced ghost)
        SaveButtonEnabled = false;

        LogHistory(SavedMessageKey);
    }

    private void Cancel()
    {
        RebuildWorkingList(); // reload from persisted store
        if (!_isActive)
        {
            return;
        }

        LogHistory(CanceledMessageKey);
    }

    private void AddFromRunning() => AddFromRunning(string.Empty);
    private void AddFromRunning(string? path = "")
    {
        if (string.IsNullOrWhiteSpace(SelectedRunning))
        {
            // ..technically not possible with variable Add button IsEnabled binding
            return;
        }

        // Handle unsaved changes first (if any)
        OnViewChanged();

        var process = SelectedRunning;
        path = string.IsNullOrWhiteSpace(path) ? ResolveProcessPath(process) : path;
        if (string.IsNullOrWhiteSpace(path))
        {
            LogHistory(PathErrorMessageKey, null, [process]);
            return;
        }

        // Guard uniqueness across Launchers/Aux/Killers
        if (!_uniqueEntryPolicy.CanAddUniqueEntry(_features.Config.Current, process, path))
        {
            LogHistory(UnableMessageKey, null, [process]);
            return;
        }

        // Hand off to dialog (edit buffer) for actual create, pre-fill dialog with needed new data
        _dialog.BeginAdd(process, path);
    }

    private void BrowseMain()
    {
        // Seed initial folder from selection if possible
        var initialDir = string.Empty;

        if (SelectedLauncher?.Model?.Entry?.Path is string p1 && !string.IsNullOrWhiteSpace(p1))
        {
            initialDir = Path.GetDirectoryName(p1) ?? string.Empty;
        }
        else if (!string.IsNullOrWhiteSpace(SelectedRunning))
        {
            var p2 = ResolveProcessPath(SelectedRunning);
            if (!string.IsNullOrWhiteSpace(p2))
            {
                initialDir = Path.GetDirectoryName(p2) ?? string.Empty;
            }
        }

        string title = _features.Localization.GetString(BrowseFileTitleKey);
        if (!FolderDialog.TryOpenFileDialog(initialDir, out var path, title))
        {
            return;
        }

        SelectedRunning = Path.GetFileNameWithoutExtension(path);

        AddFromRunning(path);
    }

    private void MoveSelectedApp(int delta)
    {
        if (SelectedLauncher == null)
        {
            return;
        }

        int idx = WorkingLaunchers.IndexOf(SelectedLauncher);
        int newIdx = idx + delta;
        if (idx < 0 || newIdx < 0 || newIdx >= WorkingLaunchers.Count)
        {
            return;
        }

        var item = WorkingLaunchers[idx];
        WorkingLaunchers.RemoveAt(idx);
        WorkingLaunchers.Insert(newIdx, item);

        // Keep selection on moved launcher item
        _squelch = true;
        SelectedLauncher = item;
        _squelch = false;

        // Check if order matches saved order
        if (IsSavedOrder())
        {
            // ...not dirty if only order changed back to saved
            SaveButtonEnabled = false;
        }
        else
        {
            MarkDirty();
        }

        RaiseRowActionCanExecutes();
    }

    private async Task EditSaved()
    {
        if (SelectedLauncher == null)
        {
            return;
        }

        if (SaveButtonEnabled)
        {
            // Handle unsaved changes first (if any)
            // Must retain selection after save/cancel
            var selectedLauncher = SelectedLauncher.Model;
            bool saveChanges = await ConfirmOrCancelChanges();
            if (saveChanges)
            {
                Save();
            }
            else
            {
                Cancel();
            }

            // Reselect previous launcher
            SelectedLauncher = new LauncherRowViewModel(selectedLauncher);
        }

        _dialog.BeginEdit(SelectedLauncher.Model); // dialog clones into its edit buffer
    }

    private void RemoveSaved()
    {
        if (SelectedLauncher == null)
        {
            return;
        }

        int idx = WorkingLaunchers.IndexOf(SelectedLauncher);
        if (idx >= 0)
        {
            WorkingLaunchers.RemoveAt(idx);
            SelectedLauncher = null;
            MarkDirty();
            RaiseRowActionCanExecutes();
        }
    }


    private bool CanMoveSelected(int delta)
    {
        var sel = SelectedLauncher;
        if (sel == null)
        {
            return false;
        }

        int idx = WorkingLaunchers.IndexOf(sel);
        if (idx < 0)
        {
            return false;
        }

        int newIdx = idx + delta;
        return newIdx >= 0 && newIdx < WorkingLaunchers.Count;
    }

    private bool IsSavedOrder()
    {
        var savedList = _features.Config.Current.Launchers ?? [];
        if (savedList.Count != WorkingLaunchers.Count)
        {
            return false;
        }

        for (int i = 0; i < WorkingLaunchers.Count; i++)
        {
            if (WorkingLaunchers[i].Model.Id != savedList[i].Id)
            {
                return false;
            }
        }

        return true;
    }

    private static string ResolveProcessPath(string processNameBare)
    {
        if (string.IsNullOrWhiteSpace(processNameBare))
        {
            return string.Empty;
        }

        return ProcessLookup.GetFirstPathForProcessName(processNameBare) ?? string.Empty;
    }

    private async Task<bool> ConfirmOrCancelChanges()
    {
        string title = _features.Localization.GetString(ChangedTitleKey);
        string message = _features.Localization.GetString(ChangedMessageKey);

        return await AppDialog.Current.ConfirmYesNoAsync(title, message);
    }

    private void MarkDirty()
    {
        SaveButtonEnabled = true;
    }


    private void LogHistory(string key) => LogHistory(key, null);
    private void LogHistory(string key, string? message = null) => LogHistory(key, message, []);
    private void LogHistory(string key, string? message = null, params object[] args)
    {
        InfoService.LogLauncherHistory(_features, key, message, args);
    }


    private void RebuildWorkingList()
    {
        WorkingLaunchers.Clear();

        var list = _features?.Config.Current.Launchers ?? [];
        foreach (var l in list)
        {
            WorkingLaunchers.Add(new LauncherRowViewModel(l));
        }

        SaveButtonEnabled = false;

        _squelch = true;
        SelectedLauncher = null;
        SelectedRunning = null;
        _squelch = false;

        RaiseRowActionCanExecutes();
    }

    private void RaiseRowActionCanExecutes()
    {
        (AddFromRunningCommand as RelayCommand)?.RaiseCanExecuteChanged();
        (EditSavedCommand as RelayCommand)?.RaiseCanExecuteChanged();
        (RemoveSavedCommand as RelayCommand)?.RaiseCanExecuteChanged();
        (MoveUpCommand as RelayCommand)?.RaiseCanExecuteChanged();
        (MoveDownCommand as RelayCommand)?.RaiseCanExecuteChanged();
    }

    private void OnViewChanged()
    {
        if (!SaveButtonEnabled)
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


    private void OnHistoryChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        _clearHistory.RaiseCanExecuteChanged();
    }


    /// <summary>
    /// Call this cleanup from the hosting Tab control when the tab is closing.<br/>
    /// Checks for changes and presents dialog for user to save or cancel.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task OnClosingAsync()
    {
        _features.Config.SaveDataUpdated -= (_, __) => RebuildWorkingList();

        if (_features.LauncherHistory.Items is INotifyCollectionChanged incc)
        {
            incc.CollectionChanged -= OnHistoryChanged;
        }

        if (!SaveButtonEnabled)
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
