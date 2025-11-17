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
/// The view model for the application's Launcher Tab Add/Edit Dialog.<br/>
/// Provides small wrappers for bindable, object-based editing
/// </summary>
public sealed class LauncherDialogViewModel : BaseViewModel
{
    private const string SavedMessageKey = "Message.LauncherDialog.History.Saved";
    private const string CanceledMessageKey = "Message.LauncherDialog.History.Canceled";

    private const string BrowseFileTitleKey = "Title.BrowseFile";

    private const string UnableTitleKey = "Title.LauncherDialog.Dialog.Unable";
    private const string UnableMessageKey = "Message.LauncherDialog.Dialog.Unable";

    private const string PathErrorTitleKey = "Title.LauncherDialog.Dialog.PathError";
    private const string PathErrorMessageKey = "Message.LauncherDialog.Dialog.PathError";

    private const string UnsavedDuplicateTitleKey = "Title.LauncherDialog.Dialog.UnsavedDuplicate";
    private const string UnsavedDuplicateMessageKey = "Message.LauncherDialog.Dialog.UnsavedDuplicate";
    private const string UnsavedDuplicateTargetMessageKey = "Message.LauncherDialog.Dialog.UnsavedDuplicate.TargetApp";


    private readonly IFeatureServices _features;
    private readonly IUniqueEntryPolicy _uniqueEntryPolicy;

    private LauncherEditorViewModel? _lastHookedEditing;
    private EntryViewModel? _lastHookedEntry;

    private LauncherEditorViewModel? _editing;

    private bool _isOpen;
    private bool _isEditing = false;
    private bool _squelch; // prevents recursive setter ping-pong
    private string? _selectedRunning;


    /// <summary>
    /// View model for a single launcher entry (main or auxiliary).
    /// </summary>
    public sealed class EntryViewModel : BaseViewModel
    {
        private string _name = string.Empty;
        private string _path = string.Empty;
        private string _arguments = string.Empty;
        private bool _enabled = true;
        private bool _detection = false;
        private bool _auxEnabled = true;
        private bool _squelch; // prevents recursive setter ping-pong



        /// <summary>
        /// Gets or sets the name for this task launcher entry.
        /// </summary>
        public string Name
        {
            get { return _name; }
            set { SetProperty(ref _name, value); }
        }

        /// <summary>
        /// Gets or sets the file path for this task launcher entry.
        /// </summary>
        public string Path
        {
            get { return _path; }
            set { SetProperty(ref _path, value); }
        }

        /// <summary>
        /// Gets or sets the command-line arguments for this task launcher entry.
        /// </summary>
        public string Arguments
        {
            get { return _arguments; }
            set { SetProperty(ref _arguments, value); }
        }

        /// <summary>
        /// Gets or sets a value indicating whether this task launcher entry is enabled.<br/>
        /// Will always follow the inverse of <see cref="Detection"/>.
        /// </summary>
        public bool Enabled
        {
            get { return _enabled; }
            set 
            {
                if (SetProperty(ref _enabled, value))
                {

                    if (!_squelch)
                    {
                        _squelch = true;
                        _detection = !value;
                        _squelch = false;
                    }
                }
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether detection mode is enabled for this entry.<br/>
        /// Will always follow the inverse of <see cref="Enabled"/>.<br/>
        /// <br/>
        /// This value is not peristed to storage, it is only for runtime use.
        /// </summary>
        public bool Detection
        {
            get { return _detection; }
            set
            {
                if (SetProperty(ref _detection, value))
                {

                    if (!_squelch)
                    {
                        _squelch = true;
                        _enabled = !value;
                        _squelch = false;
                    }
                }
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether auxiliary applications are enabled for this entry.
        /// </summary>
        public bool AuxEnabled
        {
            get { return _auxEnabled; }
            set { SetProperty(ref _auxEnabled, value); }
        }

        /// <summary>
        /// Creates an EntryViewModel from an EntryData model.
        /// </summary>
        /// <param name="model">The EntryData model.</param>
        /// <returns>A new EntryViewModel instance.</returns>
        public static EntryViewModel FromModel(EntryData? model)
        {
            model ??= new EntryData();
            return new EntryViewModel
            {
                Name = model.Name ?? string.Empty,
                Path = model.Path ?? string.Empty,
                Arguments = model.Arguments ?? string.Empty,
                Enabled = model.Enabled,
                AuxEnabled = model.AuxEnabled
            };
        }

        /// <summary>
        /// Converts the EntryViewModel back to an EntryData model.
        /// </summary>
        /// <returns>The corresponding EntryData model.</returns>
        public EntryData ToModel()
        {
            return new EntryData
            {
                Name = Name ?? string.Empty,
                Path = Path ?? string.Empty,
                Arguments = Arguments ?? string.Empty,
                Enabled = Enabled,
                AuxEnabled = AuxEnabled
            };
        }
    }

    /// <summary>
    /// View model for the launcher being edited in the dialog.
    /// </summary>
    public sealed class LauncherEditorViewModel : BaseViewModel
    {
        private EntryViewModel _entry = EntryViewModel.FromModel(null);
        private EntryViewModel? _selectedAuxApp;

        /// <summary>
        /// Gets or sets the unique identifier for the launcher entry.
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Gets or sets the main entry being edited.
        /// </summary>
        public EntryViewModel Entry
        {
            get { return _entry; }
            set { SetProperty(ref _entry, value); }
        }

        /// <summary>
        /// Gets the collection of auxiliary applications associated with the launcher.
        /// </summary>
        public ObservableCollection<EntryViewModel> AuxApps { get; } = [];

        /// <summary>
        /// Gets or sets the currently selected auxiliary application in the UI.
        /// </summary>
        public EntryViewModel? SelectedAuxApp
        {
            get { return _selectedAuxApp; }
            set
            {
                SetProperty(ref _selectedAuxApp, value);
            }
        }

        /// <summary>
        /// Creates a LauncherEditorViewModel from an existing LauncherData model.
        /// </summary>
        /// <param name="model">The existing launcher data model.</param>
        /// <returns>A new LauncherEditorViewModel instance.</returns>
        public static LauncherEditorViewModel FromExisting(LauncherData model)
        {
            var vm = new LauncherEditorViewModel
            {
                Id = model.Id != Guid.Empty ? model.Id : Guid.NewGuid(),
                Entry = EntryViewModel.FromModel(model.Entry)
            };

            if (model.AuxApps != null && model.AuxApps.Count > 0)
            {
                foreach (var a in model.AuxApps)
                {
                    vm.AuxApps.Add(EntryViewModel.FromModel(a));
                }
            }

            return vm;
        }

        /// <summary>
        /// Creates a new LauncherEditorViewModel with a new unique identifier and specified name and path.
        /// </summary>
        /// <param name="name">The name for the new entry.</param>
        /// <param name="path">The path for the new entry.</param>
        /// <returns>A new LauncherEditorViewModel instance.</returns>
        public static LauncherEditorViewModel CreateNew(string? name, string? path)
        {
            return new LauncherEditorViewModel
            {
                Id = Guid.NewGuid(),
                Entry = EntryViewModel.FromModel(new()
                {
                    Name = name ?? string.Empty,
                    Path = path ?? string.Empty
                })
            };
        }

        /// <summary>
        /// Converts the LauncherEditorViewModel back to a LauncherData model.
        /// </summary>
        /// <returns>The corresponding LauncherData model.</returns>
        public LauncherData ToModel()
        {
            return new LauncherData
            {
                Id = Id,
                Entry = Entry.ToModel(),
                AuxApps = AuxApps.Select(a => a.ToModel()).ToList()
            };
        }

        /// <summary>
        /// Selects an auxiliary application by name.
        /// </summary>
        /// <param name="name">The name of the auxiliary application to select.</param>
        public EntryViewModel? GetAux(string? name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return null;
            }

            foreach (var a in AuxApps)
            {
                if (string.Equals(a.Name, name, StringComparison.OrdinalIgnoreCase))
                {
                    return a;
                }
            }

            return null;
        }

        /// <summary>
        /// Edits an existing auxiliary application in the launcher.
        /// </summary>
        /// <param name="newAux">The new auxiliary application data.</param>
        public void EditAux(EntryViewModel newAux)
        {
            if (SelectedAuxApp == null)
            {
                return;
            }

            var idx = AuxApps.IndexOf(SelectedAuxApp);
            if (idx >= 0)
            {
                AuxApps[idx] = newAux;
                SelectedAuxApp = newAux;
            }
        }

        /// <summary>
        /// Adds a new auxiliary application to the launcher.
        /// </summary>
        /// <param name="aux">The auxiliary application to add.</param>
        public void AddAux(EntryViewModel aux)
        {
            AuxApps.Add(aux);
            SelectedAuxApp = aux;
        }

        /// <summary>
        /// Removes the currently selected auxiliary application from the launcher.
        /// </summary>
        public void RemoveSelectedAux()
        {
            if (SelectedAuxApp == null)
            {
                return;
            }

            var idx = AuxApps.IndexOf(SelectedAuxApp);
            if (idx >= 0)
            {
                AuxApps.RemoveAt(idx);
            }

            if (AuxApps.Count == 0)
            {
                SelectedAuxApp = null;
            }
            else
            {
                SelectedAuxApp = AuxApps[Math.Min(idx, AuxApps.Count - 1)];
            }
        }

        /// <summary>
        /// Moves the currently selected auxiliary application up or down in the list.
        /// </summary>
        /// <param name="delta">The number of positions to move (negative for up, positive for down).</param>
        public void MoveSelectedAux(int delta)
        {
            if (SelectedAuxApp == null)
            {
                return;
            }

            var idx = AuxApps.IndexOf(SelectedAuxApp);
            var newIdx = idx + delta;
            if (idx < 0 || newIdx < 0 || newIdx >= AuxApps.Count)
            {
                return;
            }

            var item = AuxApps[idx];
            AuxApps.RemoveAt(idx);
            AuxApps.Insert(newIdx, item);

            SelectedAuxApp = item;
        }
    }



    /// <summary>
    /// Gets or sets the editing buffer for the currently opened Add/Edit operation.
    /// </summary>
    public LauncherEditorViewModel? Editing
    {
        get { return _editing; }
        set
        {
            if (ReferenceEquals(_editing, value))
            {
                return;
            }

            var old = _editing;
            if (SetProperty(ref _editing, value))
            {
                UnhookEditing(old);
                HookEditing(_editing);
                RaiseAllCanExecutes();
            }
        }
    }

    /// <summary>
    /// Gets or sets a value indicating whether the dialog is open.
    /// </summary>
    public bool IsOpen
    {
        get { return _isOpen; }
        set { SetProperty(ref _isOpen, value); }
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
                    if (Editing != null)
                    {
                        Editing.SelectedAuxApp = null;
                    }

                    _squelch = false;
                }

                RaiseAuxCanExecutes();
            }
        }
    }


    /// <summary>
    /// Gets the command to save the edited launcher entry.
    /// </summary>
    public ICommand SaveCommand { get; }

    /// <summary>
    /// Gets the command to cancel editing and close the dialog.
    /// </summary>
    public ICommand CancelCommand { get; }

    /// <summary>
    /// Gets the command to browse for the existing target application path for editing.
    /// </summary>
    public ICommand BrowsePathCommand { get; }

    /// <summary>
    /// Gets the command to add an auxiliary application from the running processes list.
    /// </summary>
    public ICommand AuxAddFromRunningCommand { get; }

    /// <summary>
    /// Gets the command to browse for an auxiliary application path to be added.
    /// </summary>
    public ICommand AuxBrowsePathCommand { get; }

    /// <summary>
    /// Gets the command to browse for an existing auxiliary application path for editing.
    /// </summary>
    public ICommand AuxBrowseCommand { get; }

    /// <summary>
    /// Gets the command to remove the selected auxiliary application.
    /// </summary>
    public ICommand AuxRemoveCommand { get; }

    /// <summary>
    /// Gets the command to move the selected auxiliary application up in the list.
    /// </summary>
    public ICommand AuxMoveUpCommand { get; }

    /// <summary>
    /// Gets the command to move the selected auxiliary application down in the list.
    /// </summary>
    public ICommand AuxMoveDownCommand { get; }



#pragma warning disable CS8618 // To allow Design-time ctor branch

    /// <summary>
    /// Initializes a new instance of the <see cref="LauncherDialogViewModel"/> class for design-time use.
    /// </summary>
    public LauncherDialogViewModel()
    {
        if (DesignerProperties.GetIsInDesignMode(new DependencyObject()))
        {
            IsOpen = true;
        }
    }
#pragma warning restore CS8618

    /// <summary>
    /// Initializes a new instance of the <see cref="LauncherDialogViewModel"/> class.
    /// </summary>
    /// <param name="features">The features service.</param>
    /// <param name="uniqueEntryPolicy">The unique entry policy.</param>
    public LauncherDialogViewModel(
        IFeatureServices features,
        IUniqueEntryPolicy uniqueEntryPolicy)
    {
        _features = features;
        _uniqueEntryPolicy = uniqueEntryPolicy;

        BrowsePathCommand = new RelayCommand(() => BrowseEditTargetPath());
        SaveCommand = new RelayCommand(Save, CanSave);
        CancelCommand = new RelayCommand(Cancel);

        AuxAddFromRunningCommand = new RelayCommand(
            AddAuxFromRunning,
            () => !string.IsNullOrWhiteSpace(SelectedRunning));

        AuxBrowseCommand = new RelayCommand(() => BrowseAddNewAux());
        AuxBrowsePathCommand = new RelayCommand(
            () => BrowseEditAuxPath(),
            () => Editing?.SelectedAuxApp != null);

        AuxRemoveCommand = new RelayCommand(
            () => Editing?.RemoveSelectedAux(),
            () => Editing?.SelectedAuxApp != null);

        AuxMoveUpCommand = new RelayCommand(
            () => Editing?.MoveSelectedAux(-1),
            () => CanMoveSelectedAux(-1));

        AuxMoveDownCommand = new RelayCommand(
            () => Editing?.MoveSelectedAux(1),
            () => CanMoveSelectedAux(1));
    }



    /// <summary>
    /// Begins adding a new task launcher entry.
    /// </summary>
    /// <param name="name">The name for the new entry.</param>
    /// <param name="path">The path for the new entry.</param>
    public void BeginAdd(string? name, string? path)
    {
        Editing = LauncherEditorViewModel.CreateNew(name, path);
        SelectedRunning = null;
        _isEditing = false;
        IsOpen = true;
    }

    /// <summary>
    /// Begins editing an existing task launcher entry.
    /// </summary>
    /// <param name="existing">The existing task launcher data to edit.</param>
    public void BeginEdit(LauncherData existing)
    {
        Editing = LauncherEditorViewModel.FromExisting(existing);
        SelectedRunning = null;
        _isEditing = true;
        IsOpen = true;
    }

    /// <summary>
    /// Closes the dialog and drop the <see cref="Editing"/> buffer so no in-memory changes survive.
    /// </summary>
    public void CloseDialog()
    {
        UnhookEditing(_lastHookedEditing);
        Editing = null;
        IsOpen = false;
    }


    private void Save()
    {
        if (Editing == null)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(Editing.Entry.Name) || string.IsNullOrWhiteSpace(Editing.Entry.Path))
        {
            return;
        }

        _features.Config.Current.Launchers ??= [];

        var updated = Editing.ToModel();
        var existing = _features.Config.Current.Launchers.FirstOrDefault(x => x.Id == updated.Id);

        if (existing != null)
        {
            var idx = _features.Config.Current.Launchers.IndexOf(existing);
            _features.Config.Current.Launchers[idx] = updated;
        }
        else
        {
            if (!_uniqueEntryPolicy.CanAddUniqueEntry(_features.Config.Current, updated.Entry.Name, updated.Entry.Path))
            {
                return;
            }

            _features.Config.Current.Launchers.Add(updated);
        }

        _features.Config.Save(_features.Config.Current);

        LogHistory(SavedMessageKey);
        CloseDialog();
    }

    private void Cancel()
    {
        if (_isEditing)
        {
            LogHistory(CanceledMessageKey);
        }

        CloseDialog();
    }

    private void BrowseEditTargetPath()
    {
        if (Editing == null)
        {
            return;
        }

        if (!TryPickFile(Editing.Entry.Path, out var selectedPath, out var processName))
        {
            return; // canceled
        }

        if (!IsProcessPathValid(selectedPath, processName))
        {
            return;
        }

        Editing.Entry.Name = processName;
        Editing.Entry.Path = selectedPath;
    }

    private void AddAuxFromRunning()
    {
        if (Editing == null || string.IsNullOrWhiteSpace(SelectedRunning))
        {
            return;
        }

        var path = ResolveProcessPath(SelectedRunning);
        if (!IsProcessPathValid(path, SelectedRunning))
        {
            return;
        }

        var aux = EntryViewModel.FromModel(new EntryData
        {
            Name = SelectedRunning,
            Path = path
        });

        Editing.AddAux(aux);
    }

    private void BrowseAddNewAux()
    {
        if (Editing == null)
        {
            return;
        }

        var initialDir = string.Empty;
        if (SelectedRunning != null)
        {
            initialDir = ResolveProcessPath(SelectedRunning);
        }
        else if (Editing.SelectedAuxApp != null)
        {
            initialDir = Editing.SelectedAuxApp.Path;
        }

        if (!TryPickFile(initialDir, out var selectedPath, out var processName))
        {
            return; // canceled
        }

        if (!IsProcessPathValid(selectedPath, processName))
        {
            return;
        }

        var aux = EntryViewModel.FromModel(new EntryData
        {
            Name = processName,
            Path = selectedPath
        });

        Editing.AddAux(aux);
    }

    private void BrowseEditAuxPath()
    {
        if (Editing == null || Editing.SelectedAuxApp == null)
        {
            return;
        }

        if (!TryPickFile(Editing.SelectedAuxApp.Path, out var selectedPath, out var processName))
        {
            return; // canceled
        }

        if (!IsProcessPathValid(selectedPath, processName))
        {
            return;
        }

        var aux = EntryViewModel.FromModel(new EntryData
        {
            Name = processName,
            Path = selectedPath,
            Arguments = Editing.SelectedAuxApp.Arguments
        });

        Editing.EditAux(aux);
    }

    private bool TryPickFile(string seedPath, out string selectedPath, out string processName)
    {
        var initialDir = string.IsNullOrWhiteSpace(seedPath)
            ? string.Empty
            : Path.GetDirectoryName(seedPath) ?? string.Empty;

        string title = _features.Localization.GetString(BrowseFileTitleKey);
        if (!FolderDialog.TryOpenFileDialog(initialDir, out selectedPath, title))
        {
            processName = string.Empty;
            return false;
        }

        processName = Path.GetFileNameWithoutExtension(selectedPath) ?? string.Empty;
        return !string.IsNullOrWhiteSpace(processName);
    }

    private bool IsProcessPathValid(string? path, string process)
    {
        // Guard uniqueness across saved and unsaved entries on any list.
        if (string.IsNullOrWhiteSpace(path))
        {
            // Cannot resolve path for process
            var title = _features.Localization.GetString(PathErrorTitleKey);
            var message = _features.Localization.GetString(PathErrorMessageKey);
            var formattedMessage = string.Format(message, process);

            _ = AppDialog.Current.InformationOkAsync(title, formattedMessage);
            return false;
        }

        if (!_uniqueEntryPolicy.CanAddUniqueEntry(_features.Config.Current, process, path))
        {
            // Unable to add non-unique entries
            var title = _features.Localization.GetString(UnableTitleKey);
            var message = _features.Localization.GetString(UnableMessageKey);
            var formattedMessage = string.Format(message, process);

            _ = AppDialog.Current.InformationOkAsync(title, formattedMessage);
            return false;
        }

        if (Editing != null)
        {
            foreach (var a in Editing.AuxApps)
            {
                if (string.Equals(a.Path, path, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(a.Name, process, StringComparison.OrdinalIgnoreCase))
                {
                    // Unsaved Aux apps list contains this duplicate, cannot add
                    var title = _features.Localization.GetString(UnsavedDuplicateTitleKey);
                    var message = _features.Localization.GetString(UnsavedDuplicateMessageKey);
                    var formattedMessage = string.Format(message, process);

                    _ = AppDialog.Current.InformationOkAsync(title, formattedMessage);
                    return false;
                }
            }

            if (string.Equals(Editing.Entry.Path, path, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(Editing.Entry.Name, process, StringComparison.OrdinalIgnoreCase))
            {
                // Unsaved Target App entry path and name contain this duplicate, cannot add
                var title = _features.Localization.GetString(UnsavedDuplicateTitleKey);
                var message = _features.Localization.GetString(UnsavedDuplicateTargetMessageKey);
                var formattedMessage = string.Format(message, process);

                _ = AppDialog.Current.InformationOkAsync(title, formattedMessage);
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

    private bool CanSave()
    {
        // Not yet needed, but if readonly path/name textboxes become editable by user...
        var e = Editing;
        return e != null
            && !string.IsNullOrWhiteSpace(e.Entry?.Name)
            && !string.IsNullOrWhiteSpace(e.Entry?.Path);
    }

    private bool CanMoveSelectedAux(int delta)
    {
        var e = Editing;
        if (e == null || e.SelectedAuxApp == null)
        {
            return false;
        }

        int idx = e.AuxApps.IndexOf(e.SelectedAuxApp);
        if (idx < 0)
        {
            return false;
        }

        int newIdx = idx + delta;
        return newIdx >= 0 && newIdx < e.AuxApps.Count;
    }

    private void LogHistory(string key) => LogHistory(key, null);
    private void LogHistory(string key, string? message = null, params object[] args)
    {
        InfoService.LogLauncherHistory(_features, key, message, args);
    }


    private void RaiseAuxCanExecutes()
    {
        (AuxAddFromRunningCommand as RelayCommand)?.RaiseCanExecuteChanged();
        (AuxBrowsePathCommand as RelayCommand)?.RaiseCanExecuteChanged();
        (AuxRemoveCommand as RelayCommand)?.RaiseCanExecuteChanged();
        (AuxMoveUpCommand as RelayCommand)?.RaiseCanExecuteChanged();
        (AuxMoveDownCommand as RelayCommand)?.RaiseCanExecuteChanged();
    }

    private void RaiseAllCanExecutes()
    {
        RaiseAuxCanExecutes();
        (SaveCommand as RelayCommand)?.RaiseCanExecuteChanged();
    }


    // ---- Editing hookers ----
    private void HookEditing(LauncherEditorViewModel? vm)
    {
        _lastHookedEditing = vm;
        if (vm == null)
        {
            UnhookEntry(_lastHookedEntry);
            return;
        }

        vm.PropertyChanged += OnEditingPropertyChanged;

        if (vm.AuxApps is INotifyCollectionChanged incc)
        {
            incc.CollectionChanged += OnAuxAppsChanged;
        }

        HookEntry(vm.Entry);
    }

    private void UnhookEditing(LauncherEditorViewModel? vm)
    {
        if (vm == null)
        {
            return;
        }

        vm.PropertyChanged -= OnEditingPropertyChanged;

        if (vm.AuxApps is INotifyCollectionChanged incc)
        {
            incc.CollectionChanged -= OnAuxAppsChanged;
        }

        UnhookEntry(_lastHookedEntry);
    }

    private void HookEntry(EntryViewModel? entry)
    {
        _lastHookedEntry = entry;

        if (entry == null)
        {
            return;
        }

        entry.PropertyChanged += OnEntryPropertyChanged;
        (SaveCommand as RelayCommand)?.RaiseCanExecuteChanged();
    }

    private void UnhookEntry(EntryViewModel? entry)
    {
        if (entry == null)
        {
            return;
        }

        entry.PropertyChanged -= OnEntryPropertyChanged;
    }


    private void OnEditingPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(LauncherEditorViewModel.SelectedAuxApp))
        {
            if (!_squelch)
            {
                _squelch = true;
                if (Editing?.SelectedAuxApp != null)
                {
                    // aux selected => clear running selection
                    SelectedRunning = null;
                }

                _squelch = false;
            }

            RaiseAuxCanExecutes();
        }
        else if (e.PropertyName == nameof(LauncherEditorViewModel.Entry))
        {
            HookEntry(Editing?.Entry);
        }
    }

    private void OnEntryPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(EntryViewModel.Name) ||
            e.PropertyName == nameof(EntryViewModel.Path))
        {
            (SaveCommand as RelayCommand)?.RaiseCanExecuteChanged();
        }
    }

    private void OnAuxAppsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        RaiseAuxCanExecutes();
    }
}
