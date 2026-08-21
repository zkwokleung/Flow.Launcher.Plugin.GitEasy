using Flow.Launcher.Plugin.GitEasy.Models.Exceptions;
using Flow.Launcher.Plugin.GitEasy.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Flow.Launcher.Plugin.GitEasy.Services;

public sealed class DirectoryService : IDirectoryService
{
    private readonly ISettingsService _settingsService;

    public DirectoryService(ISettingsService settingsService)
    {
        _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
    }

    public List<string> GetDirectories(string path)
    {
        return Directory.GetDirectories(path).ToList();
    }

    public bool VerifyRepositoriesPath()
    {
        return _settingsService.GetSettingsOrDefault()
            .ReposPaths
            .Any(Directory.Exists);
    }

    public void CreateDirectory(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new InvalidPathException();
        }

        Directory.CreateDirectory(path);
    }

    public void CreateRepositoriesDirectory()
    {
        string repositoryPath = _settingsService.GetSettingsOrDefault()
            .ReposPaths
            .FirstOrDefault();

        if (repositoryPath == null)
        {
            throw new InvalidPathException();
        }

        CreateDirectory(repositoryPath);
    }

    public List<string> GetRepositoriesDirectories()
    {
        var result = new List<string>();

        foreach (string root in _settingsService.GetSettingsOrDefault().ReposPaths)
        {
            if (!Directory.Exists(root))
            {
                continue;
            }

            try
            {
                result.AddRange(GetDirectories(root));
            }
            catch (Exception)
            {
                // Root-level filesystem failures are isolated until repository search is redesigned.
            }
        }

        return result;
    }
}
