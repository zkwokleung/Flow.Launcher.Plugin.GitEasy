using Flow.Launcher.Plugin.GitEasy.Models.Exceptions;
using Flow.Launcher.Plugin.GitEasy.Services.Interfaces;
using Flow.Launcher.Plugin.GitEasy.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Security;
using System.Threading;
using System.Threading.Tasks;

namespace Flow.Launcher.Plugin.GitEasy.Services;

public sealed class DirectoryService : IDirectoryService
{
    private readonly ISettingsService _settingsService;
    private readonly PluginInitContext _context;

    public DirectoryService(ISettingsService settingsService, PluginInitContext context)
    {
        _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public void CreateDirectory(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new InvalidPathException();
        }

        Directory.CreateDirectory(path);
    }

    public Task<IReadOnlyList<string>> GetExistingRepositoryRootsAsync(
        CancellationToken cancellationToken)
    {
        string[] repositoryRoots = GetRepositoryRootsSnapshot();

        return Task.Run<IReadOnlyList<string>>(() =>
        {
            var result = new List<string>();

            foreach (string root in repositoryRoots)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (Directory.Exists(root))
                {
                    result.Add(root);
                }
            }

            cancellationToken.ThrowIfCancellationRequested();
            return result.ToArray();
        }, cancellationToken);
    }

    public Task<IReadOnlyList<string>> GetRepositoriesDirectoriesAsync(
        CancellationToken cancellationToken)
    {
        string[] repositoryRoots = GetRepositoryRootsSnapshot();

        return Task.Run<IReadOnlyList<string>>(() =>
        {
            var result = new List<string>();

            foreach (string root in repositoryRoots)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (!Directory.Exists(root))
                {
                    continue;
                }

                try
                {
                    var rootDirectories = new List<string>();
                    foreach (string directory in Directory.EnumerateDirectories(root))
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        rootDirectories.Add(directory);
                    }

                    rootDirectories.Sort(CompareDirectoryPaths);
                    foreach (string directory in rootDirectories)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        if (!result.Exists(existingDirectory =>
                            RepositoryPathNormalizer.AreEquivalent(existingDirectory, directory)))
                        {
                            result.Add(directory);
                        }
                    }
                }
                catch (Exception exception) when (IsRecoverableDirectoryEnumerationException(exception))
                {
                    _context.API.LogException(
                        nameof(DirectoryService),
                        $"Failed to enumerate repository root '{root}'.",
                        exception);
                }
            }

            cancellationToken.ThrowIfCancellationRequested();
            return result.ToArray();
        }, cancellationToken);
    }

    private string[] GetRepositoryRootsSnapshot()
    {
        var normalizedRoots = new List<string>();

        foreach (string configuredRoot in _settingsService.GetSettings().ReposPaths)
        {
            if (RepositoryPathNormalizer.TryNormalize(configuredRoot, out string normalizedRoot)
                && !normalizedRoots.Exists(existingRoot =>
                    RepositoryPathNormalizer.AreEquivalent(existingRoot, normalizedRoot)))
            {
                normalizedRoots.Add(normalizedRoot);
            }
        }

        return normalizedRoots.ToArray();
    }

    private static int CompareDirectoryPaths(string firstPath, string secondPath)
    {
        int caseInsensitiveComparison = StringComparer.OrdinalIgnoreCase.Compare(firstPath, secondPath);
        return caseInsensitiveComparison != 0
            ? caseInsensitiveComparison
            : StringComparer.Ordinal.Compare(firstPath, secondPath);
    }

    private static bool IsRecoverableDirectoryEnumerationException(Exception exception)
    {
        return exception is IOException
            or UnauthorizedAccessException
            or SecurityException
            or ArgumentException
            or NotSupportedException;
    }
}
