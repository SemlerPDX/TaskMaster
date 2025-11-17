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

using TaskMaster.Model;

namespace TaskMaster.Domain.Policies;

/// <summary>
/// Service to ensure that entries added to the save data are unique by name and path.
/// </summary>
public sealed class UniqueEntryPolicy : IUniqueEntryPolicy
{
    /// <summary>
    /// Determines if a new entry with the given name and path can be added to the save data without conflicts.
    /// </summary>
    /// <param name="current">The current save data.</param>
    /// <param name="name">The name of the new entry.</param>
    /// <param name="path">The path of the new entry.</param>
    /// <returns><see langword="True"/> if the entry can be added without conflicts, <see langword="false"/> otherwise.</returns>
    public bool CanAddUniqueEntry(SaveData current, string name, string path)
    {
        if (current == null)
        {
            return true;
        }

        string n = (name ?? string.Empty).Trim();
        string p = (path ?? string.Empty).Trim();

        bool ExistsInLaunchers =
            current.Launchers?.Any(l =>
                l?.Entry != null &&
                string.Equals(l.Entry.Name ?? string.Empty, n, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(l.Entry.Path ?? string.Empty, p, StringComparison.OrdinalIgnoreCase)) == true;

        bool ExistsInLaunchersAux = AuxAppsContainsEntry(current.Launchers, n, p);

        bool ExistsInKillers =
            current.Killers?.Any(k =>
                k?.Entry != null &&
                string.Equals(k.Entry.Name ?? string.Empty, n, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(k.Entry.Path ?? string.Empty, p, StringComparison.OrdinalIgnoreCase)) == true;


        return !(ExistsInLaunchers || ExistsInLaunchersAux || ExistsInKillers);
    }

    private static bool AuxAppsContainsEntry(List<LauncherData>? currentLaunchers, string n, string p)
    {
        if (currentLaunchers == null)
        {
            return false;
        }

        foreach (var launcher in currentLaunchers)
        {
            if (launcher?.AuxApps == null || launcher.AuxApps.Count <= 0)
            {
                continue;
            }

            bool existsInAux = launcher.AuxApps?.Any(k =>
                k != null &&
                string.Equals(k.Name ?? string.Empty, n, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(k.Path ?? string.Empty, p, StringComparison.OrdinalIgnoreCase)) == true;

            if (existsInAux)
            {
                return true;
            }
        }

        return false;
    }
}
