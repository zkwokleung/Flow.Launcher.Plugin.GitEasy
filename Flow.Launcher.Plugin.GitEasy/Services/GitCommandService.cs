using Flow.Launcher.Plugin.GitEasy.Models.Commands.Options;
using Flow.Launcher.Plugin.GitEasy.Models.Commands.Results;
using Flow.Launcher.Plugin.GitEasy.Models.Processes;
using Flow.Launcher.Plugin.GitEasy.Services.Interfaces;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Flow.Launcher.Plugin.GitEasy.Services;

public sealed class GitCommandService : IGitCommandService
{
    private static readonly TimeSpan CloneTimeout = TimeSpan.FromMinutes(30);
    private static readonly TimeSpan FetchTimeout = TimeSpan.FromMinutes(10);

    private readonly ISettingsService _settingsService;
    private readonly IProcessRunner _processRunner;

    public GitCommandService(
        ISettingsService settingsService,
        IProcessRunner processRunner)
    {
        _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
        _processRunner = processRunner ?? throw new ArgumentNullException(nameof(processRunner));
    }

    public async Task<GitCommandResult> CloneRepositoryAsync(
        GitCloneCommandOptions options,
        CancellationToken cancellationToken)
    {
        ProcessStartInfo startInfo = CreateGitCloneProcessStartInfo(options);
        return await RunGitCommandAsync(
            startInfo,
            CloneTimeout,
            "Git clone timed out.",
            cancellationToken).ConfigureAwait(false);
    }

    public async Task<GitCommandResult> FetchRepositoryAsync(
        string repositoryPath,
        CancellationToken cancellationToken)
    {
        ProcessStartInfo startInfo = CreateGitFetchProcessStartInfo(repositoryPath);
        return await RunGitCommandAsync(
            startInfo,
            FetchTimeout,
            "Git fetch timed out.",
            cancellationToken).ConfigureAwait(false);
    }

    private async Task<GitCommandResult> RunGitCommandAsync(
        ProcessStartInfo startInfo,
        TimeSpan timeout,
        string timeoutMessage,
        CancellationToken cancellationToken)
    {
        using CancellationTokenSource timeoutCancellationTokenSource = new(timeout);
        using CancellationTokenSource linkedCancellationTokenSource =
            CancellationTokenSource.CreateLinkedTokenSource(
                cancellationToken,
                timeoutCancellationTokenSource.Token);

        try
        {
            ProcessExecutionResult result = await _processRunner
                .RunAsync(startInfo, linkedCancellationTokenSource.Token)
                .ConfigureAwait(false);
            return ToGitCommandResult(result);
        }
        catch (OperationCanceledException exception) when (cancellationToken.IsCancellationRequested)
        {
            RethrowCallerCancellation(exception, cancellationToken);
            throw;
        }
        catch (OperationCanceledException exception) when (timeoutCancellationTokenSource.IsCancellationRequested)
        {
            throw new TimeoutException(timeoutMessage, exception);
        }
    }

    private string GetGitPath()
    {
        string gitPath = _settingsService.GetSettings().GitPath;
        if (!File.Exists(gitPath))
        {
            throw new FileNotFoundException("Git executable was not found.", gitPath);
        }

        return gitPath;
    }

    private ProcessStartInfo CreateGitCloneProcessStartInfo(GitCloneCommandOptions options)
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

        return PrepareGitCloneProcessStartInfo(
            options,
            GetGitPath(),
            destinationPath,
            destinationRoot);
    }

    private ProcessStartInfo CreateGitFetchProcessStartInfo(string repositoryPath)
    {
        if (string.IsNullOrWhiteSpace(repositoryPath))
        {
            throw new ArgumentException("Repository path cannot be empty.", nameof(repositoryPath));
        }

        return PrepareGitFetchProcessStartInfo(repositoryPath, GetGitPath());
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
        info.Environment["GIT_TERMINAL_PROMPT"] = "0";

        return info;
    }

    private static ProcessStartInfo PrepareGitFetchProcessStartInfo(
        string repositoryPath,
        string gitPath)
    {
        ProcessStartInfo info = new()
        {
            FileName = gitPath,
            WorkingDirectory = repositoryPath,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };

        info.ArgumentList.Add("fetch");
        info.Environment["GIT_TERMINAL_PROMPT"] = "0";

        return info;
    }

    private static GitCommandResult ToGitCommandResult(ProcessExecutionResult result)
    {
        return new GitCommandResult(
            result.ExitCode,
            result.StandardOutput,
            result.StandardError);
    }

    private static void RethrowCallerCancellation(
        OperationCanceledException exception,
        CancellationToken cancellationToken)
    {
        if (exception.CancellationToken != cancellationToken)
        {
            throw new OperationCanceledException(exception.Message, exception, cancellationToken);
        }
    }
}
