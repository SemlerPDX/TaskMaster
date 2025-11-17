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

namespace TaskMaster.Presentation.UI;

/// <summary>
/// Holds separate history logs for launcher and killer services.
/// </summary>
public sealed class HistoryHub : IHistoryHub, IDisposable
{
    /// <summary>
    /// The history log for the launcher service.
    /// </summary>
    public IHistoryLog Launcher { get; }

    /// <summary>
    /// The history log for the killer service.
    /// </summary>
    public IHistoryLog Killer { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="HistoryHub"/> class,
    /// </summary>
    public HistoryHub()
    {
        Launcher = new HistoryLog();
        Killer = new HistoryLog();
    }

    /// <summary>
    /// Disposes the history logs.
    /// </summary>
    public void Dispose()
    {
        (Launcher as IDisposable)?.Dispose();
        (Killer as IDisposable)?.Dispose();
    }
}
