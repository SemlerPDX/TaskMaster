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
using System.IO;
using System.Security.Principal;
using System.Text;
using System.Xml.Linq;

using TaskMaster.Application;
using TaskMaster.Model;
using TaskMaster.Services.Logging;

namespace TaskMaster.Services;


// TODO: Implement Task Maker / Operator Views/VM's (manual refresh button by a listbox)
/*
// Example tester - prints task list to debug log
private static void PrintTaskList()
{
    _ = Task.Run(() =>
    {
        // List only top level tasks tasks under \(ThisAppName)\
        var tasks = ScheduledTaskHandler.ListTasks(AppInfo.Name, includeSubfolders: false, enrichWithXml: true).ToList();

        // Example: bind to a list view later or debug-log
        foreach (var t in tasks)
        {
            var runLevelMsg = !string.IsNullOrEmpty(t.RunLevel) ? $" RunLevel:{t.RunLevel} " : string.Empty;
            Log.Trace($"Name:{t.Name} {runLevelMsg} MultipleInstancesPolicy:{t.MultipleInstancesPolicy}  Cmd:{t.Command} {t.Arguments}");
        }
    });
}
*/

/// <summary>
/// Static handler for creating, updating, removing, and querying scheduled tasks.
/// </summary>
public static class ScheduledTaskHandler
{
    private const string TaskVersion = "1.4";
    private const string XmlVersion = "1.0";
    private const string XmlEncoding = "UTF-16";
    private const string XmlNamespace = "http://schemas.microsoft.com/windows/2004/02/mit/task";


    /// <summary>
    /// Create or update a per-user task.<br/><br/>
    /// If runOnLogon is true, add a logon trigger.<br/>
    /// If runElevated is true, the task is created with RunLevel=Highest (UAC consent required once).<br/>
    /// By default, the logon trigger is for the current user only and the task will<br/>
    /// stop an existing instance before starting a new one.
    /// </summary>
    /// <param name="taskName">The name of the scheduled task.</param>
    /// <param name="executablePath">The path to the executable.</param>
    /// <param name="taskFolder">The folder of the scheduled task. If null, defaults to application name.</param>
    /// <param name="arguments">Optional arguments for the executable.</param>
    /// <param name="runOnLogon">A bool indicating whether to run the task on user logon.</param>
    /// <param name="runElevated">A bool indicating whether to run the task with elevated privileges.</param>
    /// <param name="onlyCurrentUserLogon">A bool indicating whether the logon trigger should be for the current user only.</param>
    /// <param name="stopExistingInstance">A bool indicating whether to stop an existing instance before starting a new one.</param>
    /// <exception cref="ArgumentException">Thrown when taskName or executablePath is null or whitespace.</exception>
    public static void CreateTask(
        string taskName,
        string executablePath,
        string? taskFolder = null,
        string? arguments = null,
        bool runOnLogon = false,
        bool runElevated = false,
        bool onlyCurrentUserLogon = true,
        bool stopExistingInstance = true)
    {
        if (string.IsNullOrWhiteSpace(taskName))
        {
            throw new ArgumentException("Task name is required.", nameof(taskName));
        }

        if (string.IsNullOrWhiteSpace(executablePath) || !Path.Exists(executablePath))
        {
            throw new ArgumentException("Valid executable path is required.", nameof(executablePath));
        }

        var taskPath = BuildTaskPath(taskName, taskFolder);

        // If user-specific trigger or StopExisting policy (or elevation) needed, will use XML
        var useXml =
            runElevated ||
            stopExistingInstance ||
            (runOnLogon && onlyCurrentUserLogon);

        if (useXml)
        {
            CreateOrUpdateFromXml(
                taskPath,
                executablePath,
                arguments,
                runOnLogon,
                runElevated,
                onlyCurrentUserLogon,
                stopExistingInstance);

            return;
        }

        var sc = runOnLogon ? "/SC ONLOGON" : string.Empty;
        var rl = runElevated ? "/RL HIGHEST" : string.Empty; // will prompt UAC if elevated
        var exeQuoted = Quote(executablePath);
        var tr = string.IsNullOrWhiteSpace(arguments) ? exeQuoted : $"{exeQuoted} {arguments.Trim()}";

        var argsCli = $"/Create /F {sc} /TN \"{taskPath}\" /TR {tr} {rl}";

        RunSchtasks(argsCli, requireElevation: runElevated);
    }

    /// <summary>
    /// Remove the specified scheduled task.<br/>
    /// If the task does not exist, no action is taken.
    /// </summary>
    /// <param name="taskName">The name of the scheduled task.</param>
    /// <param name="taskFolder">The folder of the scheduled task. If null, defaults to application name.</param>
    /// <param name="requireElevation">A bool indicating whether to require elevation to remove the task.</param>
    public static void TryRemoveTask(string taskName, string? taskFolder = null, bool requireElevation = false)
    {
        if (!Exists(taskName))
        {
            return;
        }

        try
        {
            var taskPath = BuildTaskPath(taskName, taskFolder);
            var args = $"/Delete /F /TN \"{taskPath}\"";
            RunSchtasks(args, requireElevation);
        }
        catch (Exception ex)
        {
            // ...let it slide and log - task removal is best-effort
            // Log here - AppHost path should never block startup if cleanup fails.
            Log.Error(ex, $"Failed to remove scheduled task '{taskName}'. " +
                "Attempt will be made again on next startup or settings change to RunAsAdmin or StartWithWindows.");
        }
    }

    /// <summary>
    /// Check if the specified scheduled task exists.
    /// </summary>
    /// <param name="taskName">The name of the scheduled task.</param>
    /// <param name="taskFolder">The folder of the scheduled task. If null, defaults to application name.</param>
    /// <returns><see langword="True"/> if the task exists; otherwise, <see langword="false"/>.</returns>
    public static bool Exists(string taskName, string? taskFolder = null)
    {
        var taskPath = BuildTaskPath(taskName, taskFolder);
        var psi = new ProcessStartInfo
        {
            FileName = "schtasks.exe",
            Arguments = $"/Query /TN \"{taskPath}\"",
            UseShellExecute = false,
            RedirectStandardOutput = false,
            RedirectStandardError = false,
            CreateNoWindow = true
        };

        using var p = Process.Start(psi);
        p?.WaitForExit();
        return p?.ExitCode == 0;
    }

    /// <summary>
    /// List scheduled tasks in the specified folder.<br/>
    /// </summary>
    /// <param name="taskFolder">The folder to list tasks from. If null, defaults to application name.</param>
    /// <param name="includeSubfolders">A bool indicating whether to include tasks from subfolders.</param>
    /// <param name="enrichWithXml">A bool indicating whether to enrich task data with XML details.</param>
    /// <returns>An enumerable of <see cref="TaskEntryData"/> representing the tasks found.</returns>
    public static IEnumerable<TaskEntryData> ListTasks(
        string? taskFolder = null,
        bool includeSubfolders = false,
        bool enrichWithXml = true)
    {
        var folder = NormalizeFolderPath(taskFolder); // i.e. "\TaskMaster\"
        var csv = RunSchtasksCapture("/Query /FO CSV /V");

        if (string.IsNullOrWhiteSpace(csv))
        {
            yield break;
        }

        using var reader = new StringReader(csv);
        var headerLine = reader.ReadLine();
        if (string.IsNullOrWhiteSpace(headerLine))
        {
            yield break;
        }

        var headers = ParseCsvLine(headerLine);
        int idxTaskName = IndexOf(headers, "TaskName", "Task Name");
        int idxStatus = IndexOf(headers, "Status");
        int idxNextRunTime = IndexOf(headers, "Next Run Time");
        int idxLastRunTime = IndexOf(headers, "Last Run Time");
        int idxLastResult = IndexOf(headers, "Last Result");
        int idxAuthor = IndexOf(headers, "Author");
        int idxComment = IndexOf(headers, "Comment", "Description"); // different Windows builds use "Comment"

        if (idxTaskName < 0)
        {
            yield break;
        }

        string? line;
        while ((line = reader.ReadLine()) != null)
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            var cols = ParseCsvLine(line);
            if (cols.Length <= idxTaskName)
            {
                continue;
            }

            var path = cols[idxTaskName]?.Trim() ?? string.Empty; // "\Folder\Task"
            if (!IsInFolder(path, folder, includeSubfolders))
            {
                continue;
            }

            var name = ExtractName(path); // "Task"
            var taskFolderActual = ExtractFolder(path); // "\Folder\"

            var entry = new TaskEntryData
            {
                Name = name,
                Path = path,
                Folder = taskFolderActual,
                Status = Get(cols, idxStatus),
                NextRunTime = Get(cols, idxNextRunTime),
                LastRunTime = Get(cols, idxLastRunTime),
                LastResult = Get(cols, idxLastResult),
                Author = Get(cols, idxAuthor),
                Description = Get(cols, idxComment)
            };

            if (enrichWithXml)
            {
                try
                {
                    EnrichFromXml(entry);
                }
                catch
                {
                    // Non-fatal: keeps CSV-only data if XML lookup fails (i.e. permissions or system task).
                }
            }

            yield return entry;
        }
    }

    /// <summary>
    /// Tries to Run the specified scheduled task.<br/>
    /// If the task does not exist, or upon any failure or error, returns <see langword="false"/>.
    /// </summary>
    /// <param name="taskName">The name of the scheduled task.</param>
    /// <param name="taskFolder">The folder of the scheduled task. If null, defaults to application name.</param>
    /// <param name="asAdmin">A bool indicating whether to run the task with elevated privileges.</param>
    /// <returns><see langword="True"/> if the task was started successfully; otherwise, <see langword="false"/>.</returns>
    public static bool TryRunTask(string taskName, string? taskFolder = null, bool asAdmin = false)
    {
        try
        {
            if (!Exists(taskName))
            {
                throw new InvalidOperationException($"Scheduled task '{taskName}' does not exist.");
            }

            var taskPath = BuildTaskPath(taskName, taskFolder);
            RunSchtasks($"/run /TN \"{taskPath}\"", asAdmin);
        }
        catch (InvalidOperationException ex)
        {
            Log.Debug($"{ex} - Unable to run scheduled task.");
            return false;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to run scheduled task.");
            return false;
        }

        return true;
    }


    private static void RunSchtasks(string arguments, bool requireElevation = false)
    {
        var psi = new ProcessStartInfo
        {
            FileName = "schtasks.exe",
            Arguments = arguments,
            UseShellExecute = true,
            Verb = requireElevation ? "runas" : string.Empty,
            WindowStyle = ProcessWindowStyle.Hidden
        };

        using var p = Process.Start(psi);
        p?.WaitForExit();
        if (p?.ExitCode != 0)
        {
            throw new InvalidOperationException($"schtasks failed with exit code {p?.ExitCode}.");
        }
    }

    private static void CreateOrUpdateFromXml(
        string taskName,
        string executablePath,
        string? arguments,
        bool runOnLogon,
        bool runElevated,
        bool onlyCurrentUserLogon,
        bool stopExistingInstance)
    {
        var exe = Unquote(executablePath); // Exec/Command cannot not be quoted in XML
        var args = arguments?.Trim() ?? string.Empty;

        // Conditional principals + trigger scope.
        string? currentSid = null;
        string principalBlock;

        if (onlyCurrentUserLogon)
        {
            // For a user-specific logon trigger, requires <LogonTrigger><UserId>SID</UserId></LogonTrigger>.
            currentSid = WindowsIdentity.GetCurrent().User?.Value
                         ?? throw new InvalidOperationException("Cannot resolve current user SID.");

            principalBlock =
$@"  <Principals>
    <Principal id=""Author"">
      <UserId>{EscapeXml(currentSid)}</UserId>
      <LogonType>InteractiveToken</LogonType>{(runElevated ? "\n      <RunLevel>HighestAvailable</RunLevel>" : string.Empty)}
    </Principal>
  </Principals>";
        }
        else
        {
            // For "any user", can just omit UserId and use Principal.GroupId = "Users". (per MS docs)
            principalBlock =
$@"  <Principals>
    <Principal id=""Author"">
      <GroupId>Users</GroupId>
      <LogonType>InteractiveToken</LogonType>{(runElevated ? "\n      <RunLevel>HighestAvailable</RunLevel>" : string.Empty)}
    </Principal>
  </Principals>";
        }

        var triggersBlock = runOnLogon
            ? (onlyCurrentUserLogon
                ? $@"  <Triggers>
    <LogonTrigger>
      <Enabled>true</Enabled>
      <UserId>{EscapeXml(currentSid!)}</UserId>
    </LogonTrigger>
  </Triggers>"
                : @"  <Triggers>
    <LogonTrigger>
      <Enabled>true</Enabled>
    </LogonTrigger>
  </Triggers>")
            : @"  <Triggers />";

        var multipleInstances = stopExistingInstance ? "<MultipleInstancesPolicy>StopExisting</MultipleInstancesPolicy>" : string.Empty;

        var xml =
$@"<?xml version=""{XmlVersion}"" encoding=""{XmlEncoding}""?>
<Task version=""{TaskVersion}"" xmlns=""{XmlNamespace}"">
  <RegistrationInfo>
    <Author>{AppInfo.Name}</Author>
    <Description>Created by {AppInfo.Name}</Description>
  </RegistrationInfo>
{principalBlock}
{triggersBlock}
  <Settings>
    <Enabled>true</Enabled>
    <AllowStartOnDemand>true</AllowStartOnDemand>
    <StartWhenAvailable>true</StartWhenAvailable>
    <ExecutionTimeLimit>PT0S</ExecutionTimeLimit>
    {multipleInstances}
  </Settings>
  <Actions Context=""Author"">
    <Exec>
      <Command>{EscapeXml(exe)}</Command>{(string.IsNullOrEmpty(args) ? string.Empty : $"\n      <Arguments>{EscapeXml(args)}</Arguments>")}
    </Exec>
  </Actions>
</Task>";

        // Write temp XML and import
        var tmp = Path.Combine(Path.GetTempPath(), $"tm_task_{Guid.NewGuid():N}.xml");
        File.WriteAllText(tmp, xml, Encoding.Unicode); // Task XML is UTF-16 in examples

        try
        {
            var cmd = $"/Create /F /TN \"{taskName}\" /XML \"{tmp}\"";
            RunSchtasks(cmd, requireElevation: runElevated);
        }
        finally
        {
            try
            {
                File.Delete(tmp);
            }
            catch
            {
                // ...let it slide
            }
        }
    }

    private static string BuildTaskPath(string taskName, string? taskFolder = null)
    {
        if (string.IsNullOrWhiteSpace(taskFolder))
        {
            taskFolder = AppInfo.Name;
        }

        var folder = taskFolder.Trim().Replace('/', '\\');

        if (!folder.StartsWith("\\", StringComparison.Ordinal))
        {
            folder = "\\" + folder;
        }

        while (folder.EndsWith("\\", StringComparison.Ordinal) && folder.Length > 1)
        {
            folder = folder.Substring(0, folder.Length - 1);
        }

        return $"{folder}\\{taskName}";
    }

    private static string Quote(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new ArgumentException("Executable path is required.", nameof(path));
        }

        return path.StartsWith("\"", StringComparison.Ordinal) ? path : $"\"{path.Trim()}\"";
    }

    private static string Unquote(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new ArgumentException("Executable path is required.", nameof(path));
        }

        var p = path.Trim();
        if (p.StartsWith("\"", StringComparison.Ordinal) && p.EndsWith("\"", StringComparison.Ordinal) && p.Length >= 2)
        {
            p = p.Substring(1, p.Length - 2);
        }

        return p;
    }

    private static string EscapeXml(string s)
    {
        return s
            .Replace("&", "&amp;")
            .Replace("<", "&lt;")
            .Replace(">", "&gt;")
            .Replace("\"", "&quot;")
            .Replace("'", "&apos;");
    }

    private static void EnrichFromXml(TaskEntryData entry)
    {
        // schtasks /Query /XML must target the specific task (no wildcards)
        var xml = RunSchtasksCapture($"/Query /TN \"{entry.Path}\" /XML");
        if (string.IsNullOrWhiteSpace(xml))
        {
            return;
        }

        var doc = XDocument.Parse(xml);
        var ns = XNamespace.Get(XmlNamespace);

        var regInfo = doc.Root?.Element(ns + "RegistrationInfo");
        var principals = doc.Root?.Element(ns + "Principals")?.Element(ns + "Principal");
        var settings = doc.Root?.Element(ns + "Settings");
        var actions = doc.Root?.Element(ns + "Actions")?.Element(ns + "Exec");

        var authorXml = regInfo?.Element(ns + "Author")?.Value?.Trim();
        var descXml = regInfo?.Element(ns + "Description")?.Value?.Trim();
        var runLevel = principals?.Element(ns + "RunLevel")?.Value?.Trim();
        var multi = settings?.Element(ns + "MultipleInstancesPolicy")?.Value?.Trim();
        var cmd = actions?.Element(ns + "Command")?.Value?.Trim();
        var args = actions?.Element(ns + "Arguments")?.Value?.Trim();

        if (!string.IsNullOrEmpty(authorXml))
        {
            entry.Author = authorXml;
        }

        if (!string.IsNullOrEmpty(descXml))
        {
            entry.Description = descXml;
        }

        if (!string.IsNullOrEmpty(runLevel))
        {
            entry.RunLevel = runLevel;
        }

        if (!string.IsNullOrEmpty(multi))
        {
            entry.MultipleInstancesPolicy = multi;
        }

        if (!string.IsNullOrEmpty(cmd))
        {
            entry.Command = cmd;
        }

        if (!string.IsNullOrEmpty(args))
        {
            entry.Arguments = args;
        }
    }

    private static bool IsInFolder(string taskPath, string normalizedFolder, bool includeSubfolders)
    {
        // normalizedFolder example: "\TaskMaster\"
        if (!taskPath.StartsWith(normalizedFolder, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (includeSubfolders)
        {
            return true;
        }

        // Require no extra backslash after the folder prefix
        var remainder = taskPath.Substring(normalizedFolder.Length);
        return !remainder.Contains('\\');
    }

    private static string ExtractName(string path)
    {
        var i = path.LastIndexOf('\\');
        if (i >= 0 && i < path.Length - 1)
        {
            return path.Substring(i + 1);
        }

        return path;
    }

    private static string ExtractFolder(string path)
    {
        var i = path.LastIndexOf('\\');
        if (i > 0)
        {
            return path.Substring(0, i + 1); // include trailing '\'
        }

        return "\\";
    }

    private static string Get(string[] cols, int index)
    {
        if (index < 0 || index >= cols.Length)
        {
            return string.Empty;
        }

        return cols[index]?.Trim() ?? string.Empty;
    }

    private static int IndexOf(string[] headers, params string[] candidates)
    {
        for (int i = 0; i < headers.Length; i++)
        {
            var h = headers[i]?.Trim();
            if (string.IsNullOrEmpty(h))
            {
                continue;
            }

            foreach (var c in candidates)
            {
                if (string.Equals(h, c, StringComparison.OrdinalIgnoreCase))
                {
                    return i;
                }
            }
        }

        return -1;
    }

    private static string NormalizeFolderPath(string? taskFolder)
    {
        // Default to app folder
        if (string.IsNullOrWhiteSpace(taskFolder))
        {
            taskFolder = AppInfo.Name;
        }

        var folder = taskFolder.Trim().Replace('/', '\\');

        if (!folder.StartsWith("\\", StringComparison.Ordinal))
        {
            folder = "\\" + folder;
        }

        if (!folder.EndsWith("\\", StringComparison.Ordinal))
        {
            folder += "\\";
        }

        // Collapse duplicate trailing slashes if any
        while (folder.Length > 2 && folder.EndsWith("\\\\", StringComparison.Ordinal))
        {
            folder = folder.Substring(0, folder.Length - 1);
        }

        return folder;
    }

    private static string RunSchtasksCapture(string arguments)
    {
        var psi = new ProcessStartInfo
        {
            FileName = "schtasks.exe",
            Arguments = arguments,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = false,
            CreateNoWindow = true
        };

        using var p = Process.Start(psi);
        if (p == null)
        {
            return string.Empty;
        }

        var output = p.StandardOutput.ReadToEnd();
        p.WaitForExit();

        return output;
    }

    private static string[] ParseCsvLine(string line)
    {
        // Minimal CSV parser to handle quotes and escaped quotes ("")
        var list = new List<string>();
        var sb = new StringBuilder();
        bool inQuotes = false;

        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];

            if (c == '"')
            {
                if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                {
                    sb.Append('"'); // escaped quote
                    i++;
                }
                else
                {
                    inQuotes = !inQuotes;
                }

                continue;
            }

            if (c == ',' && !inQuotes)
            {
                list.Add(sb.ToString());
                sb.Clear();
            }
            else
            {
                sb.Append(c);
            }
        }

        list.Add(sb.ToString());
        return list.ToArray();
    }
}
