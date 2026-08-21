using Flow.Launcher.Plugin.GitEasy.Services.Interfaces;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Security;

namespace Flow.Launcher.Plugin.GitEasy.Services;

public sealed class SystemCommandService : ISystemCommandService
{
    private const string WslLegacyPrefix = @"\\wsl$\";
    private const string WslLocalhostPrefix = @"\\wsl.localhost\";

    private readonly IProcessRunner _processRunner;

    public SystemCommandService(IProcessRunner processRunner)
    {
        _processRunner = processRunner ?? throw new ArgumentNullException(nameof(processRunner));
    }

    public void OpenExplorer(string path = "", Action onCompleted = null)
    {
        string directoryPath = GetExistingDirectoryPath(path);
        ProcessStartInfo info = new()
        {
            FileName = "explorer.exe",
            UseShellExecute = false,
            CreateNoWindow = true,
        };
        info.ArgumentList.Add(directoryPath);

        _processRunner.StartDetached(info);
        onCompleted?.Invoke();
    }

    public void OpenVsCode(string path = "", Action onCompleted = null)
    {
        OpenEditor(path, "code", onCompleted);
    }

    public void OpenCursor(string path = "", Action onCompleted = null)
    {
        OpenEditor(path, "cursor", onCompleted);
    }

    private void OpenEditor(string path, string editorCommand, Action onCompleted)
    {
        string directoryPath = GetExistingDirectoryPath(path);
        ProcessStartInfo info = TryGetWslPathParts(
            directoryPath,
            out string distribution,
            out string linuxPath)
            ? CreateWslEditorStartInfo(editorCommand, distribution, linuxPath)
            : CreateWindowsEditorStartInfo(editorCommand, directoryPath);

        _processRunner.StartDetached(info);
        onCompleted?.Invoke();
    }

    private static ProcessStartInfo CreateWslEditorStartInfo(
        string editorCommand,
        string distribution,
        string linuxPath)
    {
        ProcessStartInfo info = new()
        {
            FileName = "wsl.exe",
            UseShellExecute = false,
            CreateNoWindow = true,
            WindowStyle = ProcessWindowStyle.Hidden,
        };
        info.ArgumentList.Add("--distribution");
        info.ArgumentList.Add(distribution);
        info.ArgumentList.Add("--");
        info.ArgumentList.Add(editorCommand);
        info.ArgumentList.Add(linuxPath);

        return info;
    }

    private static ProcessStartInfo CreateWindowsEditorStartInfo(
        string editorCommand,
        string directoryPath)
    {
        ProcessStartInfo info = new()
        {
            FileName = ResolveWindowsEditorExecutable(editorCommand),
            UseShellExecute = false,
            CreateNoWindow = true,
            WindowStyle = ProcessWindowStyle.Hidden,
        };
        info.ArgumentList.Add(directoryPath);

        return info;
    }

    private static string ResolveWindowsEditorExecutable(string editorCommand)
    {
        (string executableName, string installationDirectory) = editorCommand switch
        {
            "code" => ("Code.exe", "Microsoft VS Code"),
            "cursor" => ("Cursor.exe", "Cursor"),
            _ => throw new ArgumentOutOfRangeException(
                nameof(editorCommand),
                editorCommand,
                "Unsupported editor command."),
        };

        var candidates = new List<string>();
        var seenCandidates = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        void AddCandidate(string candidate)
        {
            try
            {
                string fullPath = Path.GetFullPath(candidate);
                if (seenCandidates.Add(fullPath))
                {
                    candidates.Add(fullPath);
                }
            }
            catch (Exception exception) when (exception is ArgumentException
                                              or NotSupportedException
                                              or PathTooLongException)
            {
                // Ignore malformed PATH entries and continue with known install locations.
            }
        }

        string pathValue = Environment.GetEnvironmentVariable("PATH") ?? string.Empty;
        foreach (string entry in pathValue.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries))
        {
            string directoryPath = entry.Trim().Trim('"');
            if (directoryPath.Length == 0)
            {
                continue;
            }

            DirectoryInfo directory;
            try
            {
                directory = new DirectoryInfo(directoryPath);
            }
            catch (Exception exception) when (exception is ArgumentException
                                              or NotSupportedException
                                              or PathTooLongException)
            {
                continue;
            }

            AddCandidate(Path.Combine(directory.FullName, executableName));

            bool hasCommandShim = File.Exists(Path.Combine(directory.FullName, editorCommand))
                                  || File.Exists(Path.Combine(directory.FullName, $"{editorCommand}.cmd"))
                                  || File.Exists(Path.Combine(directory.FullName, $"{editorCommand}.bat"));
            if (!hasCommandShim)
            {
                continue;
            }

            directory = directory.Parent;
            for (int depth = 0; directory != null && depth < 4; depth++)
            {
                AddCandidate(Path.Combine(directory.FullName, executableName));
                directory = directory.Parent;
            }
        }

        string appPathsSuffix = $@"Software\Microsoft\Windows\CurrentVersion\App Paths\{executableName}";
        foreach (string root in new[] { "HKEY_CURRENT_USER", "HKEY_LOCAL_MACHINE" })
        {
            try
            {
                if (Registry.GetValue($@"{root}\{appPathsSuffix}", string.Empty, null) is string registeredPath)
                {
                    AddCandidate(registeredPath.Trim().Trim('"'));
                }
            }
            catch (Exception exception) when (exception is IOException
                                              or SecurityException
                                              or UnauthorizedAccessException)
            {
                // Registry lookup is optional; PATH and standard locations remain available.
            }
        }

        string localPrograms = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Programs");
        AddCandidate(Path.Combine(localPrograms, installationDirectory, executableName));
        AddCandidate(Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
            installationDirectory,
            executableName));
        AddCandidate(Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
            installationDirectory,
            executableName));

        foreach (string candidate in candidates)
        {
            if (File.Exists(candidate))
            {
                return candidate;
            }
        }

        throw new FileNotFoundException(
            $"The {editorCommand} editor executable could not be found.",
            executableName);
    }

    private static string GetExistingDirectoryPath(string path)
    {
        string candidate = string.IsNullOrWhiteSpace(path)
            ? Environment.CurrentDirectory
            : path;
        string directoryPath = Path.GetFullPath(candidate);

        if (!Directory.Exists(directoryPath))
        {
            throw new DirectoryNotFoundException($"Directory does not exist: {directoryPath}");
        }

        return Path.TrimEndingDirectorySeparator(directoryPath);
    }

    private static bool TryGetWslPathParts(
        string path,
        out string distribution,
        out string linuxPath)
    {
        string prefix;
        if (path.StartsWith(WslLegacyPrefix, StringComparison.OrdinalIgnoreCase))
        {
            prefix = WslLegacyPrefix;
        }
        else if (path.StartsWith(WslLocalhostPrefix, StringComparison.OrdinalIgnoreCase))
        {
            prefix = WslLocalhostPrefix;
        }
        else
        {
            distribution = string.Empty;
            linuxPath = string.Empty;
            return false;
        }

        string[] parts = path[prefix.Length..].Split(
            new[] { '\\', '/' },
            StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0)
        {
            throw new ArgumentException("A WSL path must include a distribution name.", nameof(path));
        }

        distribution = parts[0];
        linuxPath = parts.Length == 1
            ? "/"
            : "/" + string.Join('/', parts, 1, parts.Length - 1);
        return true;
    }
}
