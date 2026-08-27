using Flow.Launcher.Plugin.GitEasy.Models.Commands.Interfaces;
using Flow.Launcher.Plugin.GitEasy.Models.Commands.Results;
using Flow.Launcher.Plugin.GitEasy.Services.Interfaces;
using Flow.Launcher.Plugin.GitEasy.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Flow.Launcher.Plugin.GitEasy.Models.Commands;

public class CloneCommand : ICommand
{
    public string Key => "Clone";
    public string Title => _context.API.GetTranslation(Translations.QueryResultClone);
    public string Description => _context.API.GetTranslation(Translations.QueryResultCloneDesc);

    private PluginInitContext _context;
    private IGitCommandService _gitCommandService;
    private ISettingsService _settingsService;
    private readonly IDirectoryService _directoryService;
    private ISystemCommandService _systemCommandService;

    public CloneCommand(
        PluginInitContext context,
        ISettingsService settingsService,
        IDirectoryService directoryService,
        IGitCommandService gitCommandService,
        ISystemCommandService systemCommandService)
    {
        _context = context;
        _settingsService = settingsService;
        _directoryService = directoryService;
        _gitCommandService = gitCommandService;
        _systemCommandService = systemCommandService;
    }

    public async Task<List<Result>> ResolveAsync(
        string query,
        string actionKeyword,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        CloneQueryResult parsedQuery = CloneQueryParser.Parse(query);

        switch (parsedQuery.Status)
        {
            case CloneQueryStatus.Hint:
                return CompleteResolution(new()
                {
                    new Result
                    {
                        Title = _context.API.GetTranslation(Translations.QueryResultCloneHint),
                        IcoPath = Icons.Logo,
                        Action = _ => true,
                    }
                }, cancellationToken);

            case CloneQueryStatus.NoRepository:
                return CompleteResolution(new()
                {
                    new Result
                    {
                        Title = _context.API.GetTranslation(Translations.QueryResultCloneNoRepos),
                        IcoPath = Icons.Logo,
                        Action = _ => true
                    }
                }, cancellationToken);

            case CloneQueryStatus.Invalid:
                return CompleteResolution(GetInvalidCloneResults(), cancellationToken);

            case CloneQueryStatus.Valid:
                break;

            default:
                throw new InvalidOperationException("Unknown clone query status.");
        }

        string repository = parsedQuery.Repository;
        string location = parsedQuery.RepositoryName;
        IReadOnlyList<string> cloneArguments = parsedQuery.Arguments;

        var settings = _settingsService.GetSettings();
        OpenOption defaultPostAction = settings.OpenReposIn;
        IReadOnlyList<string> repoRoots = await _directoryService
            .GetExistingRepositoryRootsAsync(cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();

        List<Result> results = new();

        foreach (string root in repoRoots)
        {
            cancellationToken.ThrowIfCancellationRequested();
            string destinationPath = Path.Combine(root, location);
            results.Add(new Result
            {
                Title = $"{_context.API.GetTranslation(Translations.QueryResultClone)} {location} → {root}",
                SubTitle = string.Format(_context.API.GetTranslation(Translations.QueryResultCloneMsg), repository, root),
                IcoPath = Icons.Logo,
                Action = _ => StartCloneInBackground(
                    repository,
                    cloneArguments,
                    destinationPath,
                    location,
                    defaultPostAction)
            });
            results.Add(new Result
            {
                Title = $"{_context.API.GetTranslation(Translations.QueryResultCloneOpenExplorer)} ({root})",
                IcoPath = Icons.Explorer,
                Action = _ => StartCloneInBackground(
                    repository,
                    cloneArguments,
                    destinationPath,
                    location,
                    OpenOption.FileExplorer)
            });
            results.Add(new Result
            {
                Title = $"{_context.API.GetTranslation(Translations.QueryResultCloneOpenVSCode)} ({root})",
                IcoPath = Icons.VSCode,
                Action = _ => StartCloneInBackground(
                    repository,
                    cloneArguments,
                    destinationPath,
                    location,
                    OpenOption.VSCode)
            });
            results.Add(new Result
            {
                Title = $"{_context.API.GetTranslation(Translations.QueryResultCloneOpenCursor)} ({root})",
                IcoPath = Icons.Cursor,
                Action = _ => StartCloneInBackground(
                    repository,
                    cloneArguments,
                    destinationPath,
                    location,
                    OpenOption.Cursor)
            });
        }

        return CompleteResolution(results, cancellationToken);
    }

    private static List<Result> CompleteResolution(
        List<Result> results,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return results;
    }

    private bool StartCloneInBackground(
        string repositoryUrl,
        IReadOnlyList<string> arguments,
        string destinationPath,
        string location,
        OpenOption postAction)
    {
        _context.API.ShowMsg(
            _context.API.GetTranslation(Translations.QueryCloneStarted),
            string.Format(
                _context.API.GetTranslation(Translations.QueryCloneStartedMsg),
                location,
                destinationPath),
            iconPath: Icons.Logo);

        _ = ExecuteCloneAsync(repositoryUrl, arguments, destinationPath, location, postAction);
        return true;
    }

    private async Task ExecuteCloneAsync(
        string repositoryUrl,
        IReadOnlyList<string> arguments,
        string destinationPath,
        string location,
        OpenOption postAction)
    {
        try
        {
            GitCommandResult result = await _gitCommandService.CloneRepositoryAsync(
                new()
                {
                    Arguments = arguments,
                    DestinationPath = destinationPath,
                    Repo = repositoryUrl
                },
                CancellationToken.None);

            if (!result.Succeeded)
            {
                ShowCloneError(location, result);
                return;
            }

            ShowCloneCompleteMsg(location);
            await OpenRepositoryAsync(destinationPath, postAction);
        }
        catch (TimeoutException)
        {
            _context.API.ShowMsgError(
                _context.API.GetTranslation(Translations.Error),
                string.Format(
                    _context.API.GetTranslation(Translations.ErrorCloneTimeout),
                    destinationPath));
        }
        catch (Exception ex)
        {
            ShowCloneError(location, ex.Message);
        }
    }

    private void ShowCloneCompleteMsg(string location)
    {
        _context.API.ShowMsg(
            _context.API.GetTranslation(Translations.QueryCloneComplete),
            string.Format(
                _context.API.GetTranslation(Translations.QueryCloneCompleteMsg),
                location),
            iconPath: Icons.Logo);
    }

    private void ShowCloneError(string location, GitCommandResult result)
    {
        string details = CommandErrorFormatter.GetGitFailureDetails(
            result,
            _context.API.GetTranslation(Translations.ErrorGitExitCode));

        ShowCloneError(location, details);
    }

    private void ShowCloneError(string location, string details)
    {
        string message = CommandErrorFormatter.FormatWithDetails(
            string.Format(
                _context.API.GetTranslation(Translations.ErrorCloneMsg),
                location),
            details);

        _context.API.ShowMsgError(
            _context.API.GetTranslation(Translations.Error),
            message);
    }

    private async Task OpenRepositoryAsync(string destinationPath, OpenOption postAction)
    {
        try
        {
            switch (postAction)
            {
                case OpenOption.FileExplorer:
                    await _systemCommandService.OpenExplorerAsync(destinationPath);
                    break;
                case OpenOption.VSCode:
                    await _systemCommandService.OpenVsCodeAsync(destinationPath);
                    break;
                case OpenOption.Cursor:
                    await _systemCommandService.OpenCursorAsync(destinationPath);
                    break;
            }
        }
        catch (Exception exception)
        {
            ShowOpenRepositoryError(destinationPath, exception);
        }
    }

    private void ShowOpenRepositoryError(string destinationPath, Exception exception)
    {
        string message = CommandErrorFormatter.FormatWithDetails(
            string.Format(
                _context.API.GetTranslation(Translations.ErrorOpenRepository),
                destinationPath),
            exception.Message);

        _context.API.ShowMsgError(
            _context.API.GetTranslation(Translations.Error),
            message);
    }

    private List<Result> GetInvalidCloneResults()
    {
        return new()
        {
            new Result
            {
                Title = _context.API.GetTranslation(Translations.ErrorInvalidCmd),
                SubTitle = _context.API.GetTranslation(Translations.ErrorInvalidCmdMsg),
                IcoPath = Icons.Error,
                Action = _ => true
            }
        };
    }
}