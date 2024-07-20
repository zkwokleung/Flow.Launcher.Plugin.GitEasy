using Flow.Launcher.Plugin.GitEasy.Models.Commands.EventArgs;
using Flow.Launcher.Plugin.GitEasy.Models.Commands.Options;
using Flow.Launcher.Plugin.GitEasy.Services.Interfaces;
using System;
using System.Diagnostics;
using System.IO;

namespace Flow.Launcher.Plugin.GitEasy.Services;

public class GitCommandService : IGitCommandService
{
    private const string GIT_EXE = "git.exe";

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

        if (!File.Exists(GetGitExecutable(_settingService.GetSettingsOrDefault().GitPath)))
        {
            throw new Exception("git.exe not found");
        }

        Process.Start(PrepareGitCloneProcessStartInfo(options)).WaitForExit();

        OnCompleted?.Invoke();
    }

    public void FetchRepos(GitFetchCommandOptions options, Action<GitFetchCompletedEventArgs> OnCompleted = null)
    {
        if (string.IsNullOrWhiteSpace(options.RepoPath))
        {
            throw new ArgumentException("Repo can not be null or empty");
        }

        if (!File.Exists(GetGitExecutable(_settingService.GetSettingsOrDefault().GitPath)))
        {
            throw new Exception("git.exe not found");
        }

        string output = "";
        Process p = new()
        {
            StartInfo = PrepareGitFetchProcessStartInfo(options)
        };
        p.Start();
        output += p.StandardOutput.ReadToEnd();
        p.WaitForExit();
        OnCompleted?.Invoke(
            new Lazy<GitFetchCompletedEventArgs>(
                () => new GitFetchCompletedEventArgs()
                {
                    Output = output,
                }).Value
            );
    }

    private static ProcessStartInfo PrepareGitCloneProcessStartInfo(GitCloneCommandOptions options)
    {
        ProcessStartInfo info = new()
        {
            FileName = "git.exe",
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

    private static ProcessStartInfo PrepareGitFetchProcessStartInfo(GitFetchCommandOptions options)
    {
        ProcessStartInfo info = new()
        {
            FileName = "git.exe",
            WorkingDirectory = options.RepoPath,
            RedirectStandardOutput = true,
        };

        info.ArgumentList.Add("fetch");

        return info;
    }

    private static string GetGitExecutable(string path)
    {
        return $"{path}\\{GIT_EXE}";
    }
}
