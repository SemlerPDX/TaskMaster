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
/// The view model for the application's Task Killer tab.
/// </summary>
public sealed class KillerTabViewModel : BaseViewModel, ITabAware
{
    private const string ChangedTitleKey = "Title.KillerTab.Dialog.Changed";
    private const string ChangedMessageKey = "Message.KillerTab.Dialog.Changed";

    private const string BrowseFileTitleKey = "Title.BrowseFile";

    private const string SavedMessageKey = "Message.KillerTab.History.Saved";
    private const string CanceledMessageKey = "Message.KillerTab.History.Canceled";
    private const string UnableMessageKey = "Message.KillerTab.History.Unable";
    private const string UnsavedDuplicateMessageKey = "Message.KillerTab.History.UnsavedDuplicate";

    private const string MissingProcessName = "[[MISSING NAME]]";


    private readonly IFeatureServices _features;
    private readonly IUniqueEntryPolicy _uniqueEntryPolicy;


    private KillerRowViewModel? _selectedKiller;

    private string? _selectedRunning;
    private bool _saveButtonEnabled;

    private bool _isActive = false;
    private bool _squelch; // prevents recursive setter ping-pong

    private readonly RelayCommand _moveUpCommand;
    private readonly RelayCommand _moveDownCommand;
    private readonly RelayCommand _addFromRunningCommand;
    private readonly RelayCommand _removeSavedCommand;
    private readonly RelayCommand _saveCommand;
    private readonly RelayCommand _cancelCommand;
    private readonly RelayCommand _clearHistory;


    /// <summary>
    /// A row wrapper for the saved killers list.
    /// </summary>
    public sealed class KillerRowViewModel : BaseViewModel
    {
        /// <summary>
        /// The underlying model data for this row.
        /// </summary>
        public KillerData Model { get; }

        /// <summary>
        /// The display name for this killer row.
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
        /// Constructs a new KillerRowViewModel wrapping the given model.
        /// </summary>
        /// <param name="model">The underlying model data for this row.</param>
        /// <exception cref="ArgumentNullException">Thrown if model is null.</exception>
        public KillerRowViewModel(KillerData model)
        {
            Model = model ?? throw new ArgumentNullException(nameof(model));
        }
    }


    /// <summary>
    /// The selected killer row from the left listbox.
    /// </summary>
    public KillerRowViewModel? SelectedKiller
    {
        get { return _selectedKiller; }
        set
        {
            if (SetProperty(ref _selectedKiller, value))
            {
                if (!_squelch && value != null)
                {
                    _squelch = true;
                    SelectedRunning = null; // other list becomes "inactive"
                    _squelch = false;
                }

                RaiseRowActionCanExecutes();
            }
        }
    }

    /// <summary>
    /// The collection of saved task killer processes for display and selection.<br/>
    /// A "Ghost" working list bound to the left listbox; changes persisted only upon save.
    /// </summary>
    public ObservableCollection<KillerRowViewModel> WorkingKillers { get; } = [];

    /// <summary>
    /// A read-only collection of history log entries for the Task Killer tab.
    /// </summary>
    public ReadOnlyObservableCollection<string> KillerHistory
    {
        get
        {
            if (_features == null)
            {
                return new ReadOnlyObservableCollection<string>([]);
            }

            return _features.KillerHistory.Items;
        }
    }

    /// <summary>
    /// The selected running process name from the right listbox.
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
                    SelectedKiller = null; // other list becomes "inactive"
                    _squelch = false;
                }

                (AddFromRunningCommand as RelayCommand)?.RaiseCanExecuteChanged();
            }
        }
    }

    /// <summary>
    /// Indicates if the Save and Cancel buttons are enabled.
    /// </summary>
    public bool SaveButtonEnabled
    {
        get { return _saveButtonEnabled; }
        private set
        {
            if (SetProperty(ref _saveButtonEnabled, value))
            {
                _saveCommand?.RaiseCanExecuteChanged();
                _cancelCommand?.RaiseCanExecuteChanged();
            }
        }
    }


    /// <summary>
    /// Command to save changes to the killer list.
    /// </summary>
    public ICommand SaveCommand => _saveCommand;

    /// <summary>
    /// Command to cancel unsaved changes to the killer list.
    /// </summary>
    public ICommand CancelCommand => _cancelCommand;

    /// <summary>
    /// Command to add a running process as a killer.
    /// </summary>
    public ICommand AddFromRunningCommand => _addFromRunningCommand;

    /// <summary>
    /// Command to browse for a process to add as a killer.
    /// </summary>
    public ICommand BrowseCommand { get; }

    /// <summary>
    /// Command to move the selected killer up in the list.
    /// </summary>
    public ICommand MoveUpCommand => _moveUpCommand;

    /// <summary>
    /// Command to move the selected killer down in the list.
    /// </summary>
    public ICommand MoveDownCommand => _moveDownCommand;

    /// <summary>
    /// Command to remove the selected killer from the saved list.
    /// </summary>
    public ICommand RemoveSavedCommand => _removeSavedCommand;

    /// <summary>
    /// Command to clear the killer history log.
    /// </summary>
    public ICommand ClearHistory => _clearHistory;



#pragma warning disable CS8618 // To allow Design-time ctor branch
    /// <summary>
    /// Initializes a new instance of the <see cref="KillerTabViewModel"/> class for design-time use.
    /// </summary>
    public KillerTabViewModel()
    {
        if (DesignerProperties.GetIsInDesignMode(new DependencyObject()))
        {
            SaveButtonEnabled = true;
        }
    }
#pragma warning restore CS8618

    /// <summary>
    /// Initializes a new instance of the <see cref="KillerTabViewModel"/> class.
    /// </summary>
    /// <param name="services"></param>
    /// <param name="uniqueEntryPolicy"></param>
    public KillerTabViewModel(
        IFeatureServices services,
        IUniqueEntryPolicy uniqueEntryPolicy)
    {
        _features = services;
        _uniqueEntryPolicy = uniqueEntryPolicy;

        RebuildWorkingList(); // hydrate ghost from persisted data

        _features.Config.SaveDataUpdated += (_, __) => RebuildWorkingList(); // keep ghost in sync if store changes elsewhere

        _saveCommand = new RelayCommand(Save, () => SaveButtonEnabled);
        _cancelCommand = new RelayCommand(Cancel, () => SaveButtonEnabled);

        _addFromRunningCommand = new RelayCommand(AddFromRunning, () => !string.IsNullOrWhiteSpace(SelectedRunning));
        BrowseCommand = new RelayCommand(BrowseMain);

        // Up/Down only when the row can actually move (top disables Up, bottom disables Down)
        _moveUpCommand = new RelayCommand(() => MoveSelected(-1), () => CanMoveSelected(-1));
        _moveDownCommand = new RelayCommand(() => MoveSelected(1), () => CanMoveSelected(1));
        _removeSavedCommand = new RelayCommand(RemoveSaved, () => SelectedKiller != null);

        _clearHistory = new RelayCommand(
            () => _features.KillerHistory.Clear(),
            () => _features.KillerHistory.Items.Count > 0);

        if (_features.KillerHistory.Items is INotifyCollectionChanged incc)
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
        SelectedKiller = null;
        _squelch = false;
        Cancel();

        RequestScrollToBottom?.Invoke(this, EventArgs.Empty);

        // Lazy persist on clean app close and don't save on every tab switch...
        _features.Settings.Current.LastActiveTab = (int)AppTab.TaskKiller;

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
        _features.Config.Current.Killers = WorkingKillers
            .Select(k => k.Model)
            .ToList();

        _features.Config.Save(_features.Config.Current);

        // Clear dirty (starts with a synced ghost)
        SaveButtonEnabled = false;

        LogHistory(SavedMessageKey);
    }

    private void Cancel()
    {
        RebuildWorkingList();
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

        var process = SelectedRunning;
        path = string.IsNullOrWhiteSpace(path) ? ResolveProcessPath(process) : path;
        if (!_uniqueEntryPolicy.CanAddUniqueEntry(_features.Config.Current, process, path))
        {
            LogHistory(UnableMessageKey, null, [process]);
            return;
        }

        var model = new KillerData
        {
            Id = Guid.NewGuid(),
            Entry = new EntryData
            {
                Name = process,
                Path = path, // killers identify by process name, this just aids browse and uniqueness
                Enabled = true
            }
        };

        var entry = new KillerRowViewModel(model);

        // Guard uniqueness across *unsaved* entries on the killer list.
        bool isDuplicate = IsDuplicateKiller(entry);
        if (isDuplicate)
        {
            LogHistory(UnsavedDuplicateMessageKey, null, [process]);
            return;
        }

        WorkingKillers.Add(entry);

        _squelch = true;
        SelectedKiller = entry;
        _squelch = false;

        MarkDirty();
        RaiseRowActionCanExecutes();
    }

    private void BrowseMain()
    {
        // Seed initial folder from selection if possible
        var initialDir = string.Empty;

        if (SelectedKiller?.Model?.Entry?.Path is string p1 && !string.IsNullOrWhiteSpace(p1))
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

    private void MoveSelected(int delta)
    {
        if (SelectedKiller == null)
        {
            return;
        }

        int idx = WorkingKillers.IndexOf(SelectedKiller);
        int newIdx = idx + delta;
        if (idx < 0 || newIdx < 0 || newIdx >= WorkingKillers.Count)
        {
            return;
        }

        var item = WorkingKillers[idx];
        WorkingKillers.RemoveAt(idx);
        WorkingKillers.Insert(newIdx, item);

        // keep selection on moved row
        _squelch = true;
        SelectedKiller = item;
        _squelch = false;

        // check if order matches saved order
        if (IsSavedOrder())
        {
            // not dirty if only order changed back to saved
            SaveButtonEnabled = false;
        }
        else
        {
            MarkDirty();
        }

        RaiseRowActionCanExecutes();
    }

    private void RemoveSaved()
    {
        if (SelectedKiller == null)
        {
            return;
        }

        int idx = WorkingKillers.IndexOf(SelectedKiller);
        if (idx >= 0)
        {
            WorkingKillers.RemoveAt(idx);
            SelectedKiller = null;
            MarkDirty();
            RaiseRowActionCanExecutes();
        }
    }


    private bool CanMoveSelected(int delta)
    {
        var sel = SelectedKiller;
        if (sel == null)
        {
            return false;
        }

        int idx = WorkingKillers.IndexOf(sel);
        if (idx < 0)
        {
            return false;
        }

        int newIdx = idx + delta;
        return newIdx >= 0 && newIdx < WorkingKillers.Count;
    }

    private bool IsSavedOrder()
    {
        var savedList = _features.Config.Current.Killers ?? [];
        if (savedList.Count != WorkingKillers.Count)
        {
            return false;
        }

        for (int i = 0; i < WorkingKillers.Count; i++)
        {
            if (WorkingKillers[i].Model.Id != savedList[i].Id)
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

    private bool IsDuplicateKiller(KillerRowViewModel vm)
    {
        var name = vm.Model.Entry?.Name ?? string.Empty;
        return WorkingKillers.Any(k =>
            k != vm &&
            string.Equals(k.Model.Entry?.Name, name, StringComparison.OrdinalIgnoreCase));
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
    private void LogHistory(string key, string? message = null, params object[] args)
    {
        InfoService.LogKillerHistory(_features, key, message, args);
    }


    private void RebuildWorkingList()
    {
        WorkingKillers.Clear();

        var list = _features?.Config.Current.Killers ?? [];
        foreach (var k in list)
        {
            WorkingKillers.Add(new KillerRowViewModel(k));
        }

        // fresh load => nothing dirty
        SaveButtonEnabled = false;

        // and no selection
        _squelch = true;
        SelectedKiller = null;
        SelectedRunning = null;
        _squelch = false;

        RaiseRowActionCanExecutes();

    }

    private void RaiseRowActionCanExecutes()
    {
        (MoveUpCommand as RelayCommand)?.RaiseCanExecuteChanged();
        (MoveDownCommand as RelayCommand)?.RaiseCanExecuteChanged();
        (RemoveSavedCommand as RelayCommand)?.RaiseCanExecuteChanged();
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

        if (_features.KillerHistory.Items is INotifyCollectionChanged incc)
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
