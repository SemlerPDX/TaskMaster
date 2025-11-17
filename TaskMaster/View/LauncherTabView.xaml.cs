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

using TaskMaster.Presentation.Behaviors;
using TaskMaster.ViewModel;

using UserControl = System.Windows.Controls.UserControl;

namespace TaskMaster.View;

/// <summary>
/// Interaction logic for LauncherTabView.xaml
/// </summary>
public partial class LauncherTabView : UserControl
{
    private LauncherTabViewModel? _vm;

    /// <summary>
    /// Initializes a new instance of the <see cref="LauncherTabView"/> class.
    /// </summary>
    public LauncherTabView()
    {
        InitializeComponent();

        DataContextChanged += (_, e) => Rewire(e.OldValue as LauncherTabViewModel, e.NewValue as LauncherTabViewModel);
        Unloaded += (_, __) => Rewire(_vm, null);
        Loaded += (_, __) =>
        {
            if (DataContext is LauncherTabViewModel vm)
            {
                Rewire(null, vm);
            }
        };
    }

    private void Rewire(LauncherTabViewModel? oldVm, LauncherTabViewModel? newVm)
    {
        if (oldVm != null)
        {
            oldVm.RequestScrollToBottom -= OnRequestScrollToBottom;
        }

        _vm = newVm;

        if (newVm != null)
        {
            newVm.RequestScrollToBottom += OnRequestScrollToBottom;
        }
    }

    private void OnRequestScrollToBottom(object? sender, EventArgs e) =>
        ListBoxAutoScroll.ForceScrollToEnd(LbLauncherHistory);
}
