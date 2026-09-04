using Flow.Launcher.Plugin.GitEasy.Models;
using Flow.Launcher.Plugin.GitEasy.Services.Interfaces;
using Flow.Launcher.Plugin.GitEasy.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Flow.Launcher.Plugin.GitEasy.Services;

public sealed class SettingsService : ISettingsService
{
    private readonly PluginInitContext _context;
    private readonly Settings _settings;

    public SettingsService(PluginInitContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _settings = _context.API.LoadSettingJsonStorage<Settings>() ?? new Settings();

        if (NormalizeSettings(discoverGit: true))
        {
            try
            {
                PersistSettings();
            }
            catch (Exception exception)
            {
                _context.API.LogException(
                    nameof(SettingsService),
                    "Failed to persist normalized settings during initialization.",
                    exception);
            }
        }
    }

    public Settings GetSettings()
    {
        return _settings;
    }

    public void SaveSettings()
    {
        NormalizeSettings(discoverGit: false);
        PersistSettings();
    }

    private bool NormalizeSettings(bool discoverGit)
    {
        bool canonicalPathsWereLoaded = _settings.ReposPathsWasDeserialized;
        bool hadLegacyPath = _settings.TryConsumeLegacyRepositoryPath(out string legacyPath);
        IEnumerable<string> configuredPaths = _settings.ReposPaths;

        if (!canonicalPathsWereLoaded && hadLegacyPath)
        {
            configuredPaths = new[] { legacyPath };
        }

        List<string> normalizedPaths = RepositoryPathNormalizer.NormalizeDistinct(configuredPaths);
        bool changed = hadLegacyPath
                       || !_settings.ReposPaths.SequenceEqual(normalizedPaths, StringComparer.Ordinal)
                       || _settings.ReposPathsWasNull
                       || !canonicalPathsWereLoaded;

        _settings.ReposPaths = normalizedPaths;

        if (!Enum.IsDefined(_settings.OpenReposIn))
        {
            _settings.OpenReposIn = OpenOption.None;
            changed = true;
        }

        string trimmedGitPath = (_settings.GitPath ?? string.Empty).Trim();
        string normalizedGitPath = discoverGit
            ? DiscoverGitExecutable(trimmedGitPath) ?? trimmedGitPath
            : trimmedGitPath;

        if (!string.Equals(_settings.GitPath, normalizedGitPath, StringComparison.Ordinal))
        {
            _settings.GitPath = normalizedGitPath;
            changed = true;
        }

        return changed;
    }

    private void PersistSettings()
    {
        _context.API.SaveSettingJsonStorage<Settings>();
    }

    private static string DiscoverGitExecutable(string configuredPath)
    {
        if (IsGitExecutable(configuredPath))
        {
            return Path.GetFullPath(configuredPath);
        }

        foreach (string candidate in GetGitCandidates())
        {
            if (IsGitExecutable(candidate))
            {
                return Path.GetFullPath(candidate);
            }
        }

        return null;
    }

    private static IEnumerable<string> GetGitCandidates()
    {
        var candidates = new List<string>();
        var seenCandidates = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        string pathValue = Environment.GetEnvironmentVariable("PATH") ?? string.Empty;
        foreach (string entry in pathValue.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries))
        {
            AddCandidate(candidates, seenCandidates, Path.Combine(entry.Trim().Trim('"'), "git.exe"));
        }

        AddGitInstallCandidates(candidates, seenCandidates, Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles));
        AddGitInstallCandidates(candidates, seenCandidates, Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86));

        string localPrograms = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Programs");
        AddGitInstallCandidates(candidates, seenCandidates, localPrograms);

        return candidates;
    }

    private static void AddGitInstallCandidates(
        ICollection<string> candidates,
        ISet<string> seenCandidates,
        string basePath)
    {
        if (string.IsNullOrWhiteSpace(basePath))
        {
            return;
        }

        AddCandidate(candidates, seenCandidates, Path.Combine(basePath, "Git", "cmd", "git.exe"));
        AddCandidate(candidates, seenCandidates, Path.Combine(basePath, "Git", "bin", "git.exe"));
    }

    private static void AddCandidate(
        ICollection<string> candidates,
        ISet<string> seenCandidates,
        string candidate)
    {
        if (RepositoryPathNormalizer.TryNormalize(candidate, out string normalizedCandidate)
            && seenCandidates.Add(normalizedCandidate))
        {
            candidates.Add(normalizedCandidate);
        }
    }

    private static bool IsGitExecutable(string path)
    {
        return !string.IsNullOrWhiteSpace(path)
               && Path.IsPathFullyQualified(path)
               && string.Equals(Path.GetFileName(path), "git.exe", StringComparison.OrdinalIgnoreCase)
               && File.Exists(path);
    }
}
