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

using UserControl = System.Windows.Controls.UserControl;

namespace TaskMaster.View;

/// <summary>
/// Interaction logic for LauncherDialogView.xaml
/// </summary>
public partial class LauncherDialogView : UserControl
{
    /// <summary>
    /// Initializes a new instance of the <see cref="LauncherDialogView"/> class.
    /// </summary>
    public LauncherDialogView()
    {
        InitializeComponent();
    }

    private void TargetEnabled_Checked(object sender, System.Windows.RoutedEventArgs e)
    {
        TargetDetector.IsChecked = false;
    }

    private void TargetEnabled_UnChecked(object sender, System.Windows.RoutedEventArgs e)
    {
        TargetDetector.IsChecked = true;
    }


    private void TargetDetector_Checked(object sender, System.Windows.RoutedEventArgs e)
    {
        TargetLauncher.IsChecked = false;
    }

    private void TargetDetector_UnChecked(object sender, System.Windows.RoutedEventArgs e)
    {
        TargetLauncher.IsChecked = true;
    }
}
