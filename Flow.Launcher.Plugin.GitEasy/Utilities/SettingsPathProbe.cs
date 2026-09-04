using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace Flow.Launcher.Plugin.GitEasy.Utilities;

internal enum FileSystemPathKind
{
    Unavailable,
    File,
    Directory,
}

internal sealed record SettingsPathProbeRequest(
    string GitPath,
    IReadOnlyCollection<string> RepositoryPaths);

internal sealed record SettingsPathProbeResult(
    string GitPath,
    bool GitPathExists,
    IReadOnlyDictionary<string, FileSystemPathKind> RepositoryPaths);

internal static class SettingsPathProbe
{
    private const uint DriveRemote = 4;

    public static Task<SettingsPathProbeResult> ProbeAsync(
        SettingsPathProbeRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        return Task.Run(
            () => Probe(
                request.GitPath ?? string.Empty,
                request.RepositoryPaths,
                cancellationToken),
            cancellationToken);
    }

    private static SettingsPathProbeResult Probe(
        string gitPath,
        IEnumerable<string> repositoryPaths,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        bool gitPathExists = !string.IsNullOrEmpty(gitPath)
                             && !IsRemotePath(gitPath)
                             && File.Exists(gitPath);
        var pathKinds = new Dictionary<string, FileSystemPathKind>(StringComparer.Ordinal);

        foreach (string repositoryPath in repositoryPaths)
        {
            cancellationToken.ThrowIfCancellationRequested();

            FileSystemPathKind kind = IsRemotePath(repositoryPath)
                ? FileSystemPathKind.Unavailable
                : File.Exists(repositoryPath)
                    ? FileSystemPathKind.File
                    : Directory.Exists(repositoryPath)
                        ? FileSystemPathKind.Directory
                        : FileSystemPathKind.Unavailable;
            pathKinds[repositoryPath] = kind;
        }

        cancellationToken.ThrowIfCancellationRequested();
        return new SettingsPathProbeResult(gitPath, gitPathExists, pathKinds);
    }

    private static bool IsRemotePath(string path)
    {
        if (path.StartsWith(@"\\", StringComparison.Ordinal))
        {
            return true;
        }

        string root = Path.GetPathRoot(path);
        return !string.IsNullOrEmpty(root) && GetDriveType(root) == DriveRemote;
    }

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
    private static extern uint GetDriveType(string rootPathName);
}
