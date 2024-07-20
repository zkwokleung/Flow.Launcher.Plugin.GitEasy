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
        string path = _settingsService.GetSettingsOrDefault().ReposPath;

        if (string.IsNullOrWhiteSpace(path))
        {
            return false;
        }

        if (!Directory.Exists(path))
        {
            return false;
        }

        return true;
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
}
