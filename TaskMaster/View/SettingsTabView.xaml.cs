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

using System.Text.RegularExpressions;
using System.Windows.Input;

using UserControl = System.Windows.Controls.UserControl;

namespace TaskMaster.View;

/// <summary>
/// Interaction logic for SettingsTabView.xaml
/// </summary>
public partial class SettingsTabView : UserControl
{
    private readonly Regex _regexInteger;
    private readonly Regex _regexOneDecimal;

    /// <summary>
    /// Initializes a new instance of the <see cref="SettingsTabView"/> class.
    /// </summary>
    public SettingsTabView()
    {
        InitializeComponent();
        _regexInteger = IntegerInputRegEx();
        _regexOneDecimal = DecimalInputRegEx();
    }


    /// <summary>
    /// Allows only integer input with up to 6 digits in a TextBox.
    /// </summary>
    /// <param name="sender">The TextBox</param>
    /// <param name="e">The TextCompositionEventArgs</param>
    public void IntegerOnly_PreviewTextInput(object sender, TextCompositionEventArgs e)
    {
        var textBox = (System.Windows.Controls.TextBox)sender;
        string proposed = textBox.Text.Remove(textBox.SelectionStart, textBox.SelectionLength)
                                      .Insert(textBox.SelectionStart, e.Text);

        e.Handled = !_regexInteger.IsMatch(proposed) || proposed.Length > 6;
    }

    /// <summary>
    /// Allows only numeric input with up to one decimal place in a TextBox
    /// </summary>
    /// <param name="sender">The TextBox</param>
    /// <param name="e">The TextCompositionEventArgs</param>
    public void NumericWithOneDecimal_PreviewTextInput(object sender, TextCompositionEventArgs e)
    {
        var textBox = (System.Windows.Controls.TextBox)sender;
        string proposed = textBox.Text.Remove(textBox.SelectionStart, textBox.SelectionLength)
                                      .Insert(textBox.SelectionStart, e.Text);

        e.Handled = !_regexOneDecimal.IsMatch(proposed) || proposed.Length > 3;
    }

    [GeneratedRegex(@"^[0-9]+$", RegexOptions.Compiled)]
    private partial Regex IntegerInputRegEx();

    [GeneratedRegex(@"^[0-9]+(\.[0-9]+)?$", RegexOptions.Compiled)]
    private partial Regex DecimalInputRegEx();
}
