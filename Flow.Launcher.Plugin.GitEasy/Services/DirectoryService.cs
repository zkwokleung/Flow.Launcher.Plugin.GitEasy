using Flow.Launcher.Plugin.GitEasy.Models.Exceptions;
using Flow.Launcher.Plugin.GitEasy.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Flow.Launcher.Plugin.GitEasy.Services;

public class DirectoryService : IDirectoryService
{
    private ISettingsService _settingsService;

    public DirectoryService(ISettingsService settingsService)
    {
        _settingsService = settingsService;
    }

    public List<string> GetDirectories(string path)
    {
        return Directory.GetDirectories(path).ToList();
    }

    public bool VerifyRepositoriesPath()
    {
        List<string> paths = _settingsService.GetSettingsOrDefault().ReposPaths;

        if (paths == null || paths.Count == 0)
        {
            return false;
        }

        return paths.Any(p => !string.IsNullOrWhiteSpace(p) && Directory.Exists(p));
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
        CreateDirectory(_settingsService.GetSettingsOrDefault().ReposPath);
    }

    public List<string> GetRepositoriesDirectories()
    {
        List<string> result = new();
        foreach (string root in _settingsService.GetSettingsOrDefault().ReposPaths ?? new())
        {
            if (!string.IsNullOrWhiteSpace(root) && Directory.Exists(root))
            {
                try
                {
                    result.AddRange(GetDirectories(root));
                }
                catch (Exception)
                {
                    // ignore invalid directories
                }
            }
        }
        return result;
    }
}
