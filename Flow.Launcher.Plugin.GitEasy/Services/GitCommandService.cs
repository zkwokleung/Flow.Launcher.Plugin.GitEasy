using Flow.Launcher.Plugin.GitEasy.Models.Commands.EventArgs;
using Flow.Launcher.Plugin.GitEasy.Models.Commands.Options;
using Flow.Launcher.Plugin.GitEasy.Services.Interfaces;
using System;
using System.Diagnostics;
using System.IO;

namespace Flow.Launcher.Plugin.GitEasy.Services;

public class GitCommandService : IGitCommandService
{

    private readonly ISettingsService _settingService;

    public GitCommandService(ISettingsService settingsService)
    {
        _settingService = settingsService;
    }

    public void CloneRepos(GitCloneCommandOptions options, Action OnCompleted = null)
    {
        if (string.IsNullOrWhiteSpace(options.Repo))
        {
            throw new ArgumentException("Repo can not be null or empty");
        }

        var gitPath = _settingService.GetSettingsOrDefault().GitPath;
        if (!File.Exists(gitPath))
        {
            throw new Exception("git.exe not found");
        }

        Process.Start(PrepareGitCloneProcessStartInfo(options, gitPath)).WaitForExit();

        OnCompleted?.Invoke();
    }

    public void FetchRepos(GitFetchCommandOptions options, Action<GitFetchCompletedEventArgs> OnCompleted = null)
    {
        if (string.IsNullOrWhiteSpace(options.RepoPath))
        {
            throw new ArgumentException("Repo can not be null or empty");
        }

        var gitPath = _settingService.GetSettingsOrDefault().GitPath;
        if (!File.Exists(gitPath))
        {
            throw new Exception("git.exe not found");
        }

        Process p = new()
        {
            StartInfo = PrepareGitFetchProcessStartInfo(options, gitPath)
        };
        p.Start();
        p.WaitForExit();
        OnCompleted?.Invoke(new()
        {
            ExitCode = p.ExitCode,
        });
    }

    private static ProcessStartInfo PrepareGitCloneProcessStartInfo(GitCloneCommandOptions options, string gitPath="git.exe")
    {
        ProcessStartInfo info = new()
        {
            FileName = gitPath,
            WorkingDirectory = options.DestinationFolder
        };

        info.ArgumentList.Add("clone");

        if (string.IsNullOrWhiteSpace(options.Options))
        {
            info.ArgumentList.Add(options.Options);
        }

        info.ArgumentList.Add(options.Repo);

        return info;
    }

    private static ProcessStartInfo PrepareGitFetchProcessStartInfo(GitFetchCommandOptions options, string gitPath = "git.exe")
    {
        ProcessStartInfo info = new()
        {
            FileName = gitPath,
            WorkingDirectory = options.RepoPath,
        };

        info.ArgumentList.Add("fetch");

        return info;
    }

}
