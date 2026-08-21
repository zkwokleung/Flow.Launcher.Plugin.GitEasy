using Flow.Launcher.Plugin.GitEasy.Models.Commands.EventArgs;
using Flow.Launcher.Plugin.GitEasy.Models.Commands.Options;
using Flow.Launcher.Plugin.GitEasy.Models.Commands.Results;
using Flow.Launcher.Plugin.GitEasy.Models.Processes;
using Flow.Launcher.Plugin.GitEasy.Services.Interfaces;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace Flow.Launcher.Plugin.GitEasy.Services;

public sealed class GitCommandService : IGitCommandService
{
    private readonly ISettingsService _settingsService;
    private readonly IProcessRunner _processRunner;

    public GitCommandService(
        ISettingsService settingsService,
        IProcessRunner processRunner)
    {
        _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
        _processRunner = processRunner ?? throw new ArgumentNullException(nameof(processRunner));
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

        string gitPath = GetGitPath();
        ProcessStartInfo startInfo = PrepareGitCloneProcessStartInfo(
            options,
            gitPath,
            destinationPath,
            destinationRoot);
        ProcessExecutionResult result = _processRunner.Run(startInfo);

        return new GitCommandResult(
            result.ExitCode,
            result.StandardOutput,
            result.StandardError);
    }

    public void FetchRepos(
        GitFetchCommandOptions options,
        Action<GitFetchCompletedEventArgs> onCompleted = null)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (string.IsNullOrWhiteSpace(options.RepoPath))
        {
            throw new ArgumentException("Repository path cannot be empty.", nameof(options));
        }

        string gitPath = GetGitPath();
        ProcessExecutionResult result = _processRunner.Run(
            PrepareGitFetchProcessStartInfo(options, gitPath));

        onCompleted?.Invoke(new GitFetchCompletedEventArgs
        {
            ExitCode = result.ExitCode,
            Output = string.IsNullOrWhiteSpace(result.StandardError)
                ? result.StandardOutput
                : result.StandardError,
        });
    }

    private string GetGitPath()
    {
        string gitPath = _settingsService.GetSettingsOrDefault().GitPath;
        if (!File.Exists(gitPath))
        {
            throw new FileNotFoundException("Git executable was not found.", gitPath);
        }

        return gitPath;
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
            RedirectStandardError = true,
        };

        info.ArgumentList.Add("clone");

        foreach (string argument in options.Arguments)
        {
            if (string.IsNullOrWhiteSpace(argument) || argument == "--")
            {
                throw new ArgumentException(
                    "Clone arguments cannot be empty or contain the end-of-options delimiter.",
                    nameof(options));
            }

            info.ArgumentList.Add(argument);
        }

        info.ArgumentList.Add("--");
        info.ArgumentList.Add(options.Repo);
        info.ArgumentList.Add(destinationPath);

        return info;
    }

    private static ProcessStartInfo PrepareGitFetchProcessStartInfo(
        GitFetchCommandOptions options,
        string gitPath)
    {
        ProcessStartInfo info = new()
        {
            FileName = gitPath,
            WorkingDirectory = options.RepoPath,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };

        info.ArgumentList.Add("fetch");

        return info;
    }
}
