using Flow.Launcher.Plugin.GitEasy.Models.Commands.EventArgs;
using Flow.Launcher.Plugin.GitEasy.Models.Commands.Options;
using Flow.Launcher.Plugin.GitEasy.Models.Commands.Results;
using Flow.Launcher.Plugin.GitEasy.Services.Interfaces;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Flow.Launcher.Plugin.GitEasy.Services;

public class GitCommandService : IGitCommandService
{

    private readonly ISettingsService _settingService;

    public GitCommandService(ISettingsService settingsService)
    {
        _settingService = settingsService;
    }

    public GitCommandResult CloneRepos(GitCloneCommandOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (string.IsNullOrWhiteSpace(options.Repo))
        {
            throw new ArgumentException("Repository URL cannot be empty.", nameof(options));
        }

        if (options.Arguments is null)
        {
            throw new ArgumentException("Clone arguments cannot be null.", nameof(options));
        }

        if (string.IsNullOrWhiteSpace(options.DestinationPath))
        {
            throw new ArgumentException("Destination path cannot be empty.", nameof(options));
        }

        string destinationPath = Path.GetFullPath(options.DestinationPath);
        string destinationRoot = Path.GetDirectoryName(destinationPath)
            ?? throw new ArgumentException("Destination path must have a parent directory.", nameof(options));

        if (!Directory.Exists(destinationRoot))
        {
            throw new DirectoryNotFoundException($"Repository root does not exist: {destinationRoot}");
        }

        if (File.Exists(destinationPath))
        {
            throw new IOException($"Clone destination is an existing file: {destinationPath}");
        }

        if (Directory.Exists(destinationPath) && Directory.EnumerateFileSystemEntries(destinationPath).Any())
        {
            throw new IOException($"Clone destination is not empty: {destinationPath}");
        }

        string gitPath = _settingService.GetSettingsOrDefault().GitPath;
        if (!File.Exists(gitPath))
        {
            throw new FileNotFoundException("Git executable was not found.", gitPath);
        }

        ProcessStartInfo startInfo = PrepareGitCloneProcessStartInfo(options, gitPath, destinationPath, destinationRoot);
        return RunGitProcess(startInfo);
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

    private static ProcessStartInfo PrepareGitCloneProcessStartInfo(
        GitCloneCommandOptions options,
        string gitPath,
        string destinationPath,
        string destinationRoot)
    {
        ProcessStartInfo info = new()
        {
            FileName = gitPath,
            WorkingDirectory = destinationRoot,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };

        info.ArgumentList.Add("clone");

        foreach (string argument in options.Arguments)
        {
            if (string.IsNullOrWhiteSpace(argument) || argument == "--")
            {
                throw new ArgumentException("Clone arguments cannot be empty or contain the end-of-options delimiter.", nameof(options));
            }

            info.ArgumentList.Add(argument);
        }

        info.ArgumentList.Add("--");
        info.ArgumentList.Add(options.Repo);
        info.ArgumentList.Add(destinationPath);

        return info;
    }

    private static GitCommandResult RunGitProcess(ProcessStartInfo startInfo)
    {
        using Process process = new()
        {
            StartInfo = startInfo
        };

        if (!process.Start())
        {
            throw new InvalidOperationException("Git process could not be started.");
        }

        Task<string> standardOutput = process.StandardOutput.ReadToEndAsync();
        Task<string> standardError = process.StandardError.ReadToEndAsync();

        process.WaitForExit();

        return new GitCommandResult(
            process.ExitCode,
            standardOutput.GetAwaiter().GetResult(),
            standardError.GetAwaiter().GetResult());
    }

    private static ProcessStartInfo PrepareGitFetchProcessStartInfo(GitFetchCommandOptions options, string gitPath = "git.exe")
    {
        ProcessStartInfo info = new()
        {
            FileName = gitPath,
            WorkingDirectory = options.RepoPath,
            CreateNoWindow = true
        };

        info.ArgumentList.Add("fetch");

        return info;
    }

}
