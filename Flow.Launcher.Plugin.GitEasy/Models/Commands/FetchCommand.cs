using Flow.Launcher.Plugin.GitEasy.Models.Commands.Interfaces;
using Flow.Launcher.Plugin.GitEasy.Models.Commands.Options;
using Flow.Launcher.Plugin.GitEasy.Models.Commands.Results;
using Flow.Launcher.Plugin.GitEasy.Services.Interfaces;
using Flow.Launcher.Plugin.GitEasy.Utilities;
using FuzzySharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Flow.Launcher.Plugin.GitEasy.Models.Commands;

public class FetchCommand : ICommand
{
    private const int MaxDiagnosticLength = 1000;

    public string Key => "Fetch";
    public string Title => _context.API.GetTranslation(Translations.QueryResultFetch);
    public string Description => _context.API.GetTranslation(Translations.QueryResultFetchDesc);
    public string IconPath => Icons.Logo;

    private readonly PluginInitContext _context;
    private readonly IGitCommandService _gitCommandService;
    private readonly IDirectoryService _directoryService;

    public FetchCommand(
        PluginInitContext context,
        IGitCommandService gitCommandService,
        IDirectoryService directoryService)
    {
        _context = context;
        _gitCommandService = gitCommandService;
        _directoryService = directoryService;
    }

    public List<Result> Resolve(string query, string actionKeyword)
    {
        List<string> directories = _directoryService.GetRepositoriesDirectories();

        return directories.Select(directory =>
        {
            string repositoryName = DirectoryUtils.ExtractRepositoryNameFromDirectory(directory);
            int score = Fuzz.Ratio(directory, query);

            return new Result
            {
                Title = repositoryName,
                SubTitle = string.Format(
                    _context.API.GetTranslation(Translations.QueryResultOpenMsg),
                    repositoryName),
                IcoPath = IconPath,
                Score = score,
                AutoCompleteText = !string.IsNullOrEmpty(actionKeyword)
                    ? $"{actionKeyword} {Key} {repositoryName}"
                    : $"{Key} {repositoryName}",
                AsyncAction = async _ =>
                {
                    await ExecuteFetchAsync(directory, repositoryName);
                    return true;
                }
            };
        }).ToList();
    }

    private async Task ExecuteFetchAsync(string repositoryPath, string repositoryName)
    {
        try
        {
            GitCommandResult result = await _gitCommandService.FetchRepositoryAsync(
                new GitFetchCommandOptions
                {
                    RepoPath = repositoryPath
                },
                CancellationToken.None);

            if (!result.Succeeded)
            {
                ShowFetchError(repositoryName, GetGitErrorDetails(result));
                return;
            }

            _context.API.ShowMsg(
                _context.API.GetTranslation(Translations.QueryFetchComplete),
                string.Format(
                    _context.API.GetTranslation(Translations.QueryFetchCompleteMsg),
                    repositoryName),
                iconPath: IconPath);
        }
        catch (TimeoutException)
        {
            _context.API.ShowMsgError(
                _context.API.GetTranslation(Translations.Error),
                string.Format(
                    _context.API.GetTranslation(Translations.ErrorFetchTimeout),
                    repositoryName));
        }
        catch (Exception exception)
        {
            ShowFetchError(repositoryName, exception.Message);
        }
    }

    private void ShowFetchError(string repositoryName, string details)
    {
        details = NormalizeDiagnostic(details);
        string message = string.Format(
            _context.API.GetTranslation(Translations.ErrorFetchMsg),
            repositoryName);

        if (!string.IsNullOrWhiteSpace(details))
        {
            message += $"{Environment.NewLine}{details}";
        }

        _context.API.ShowMsgError(
            _context.API.GetTranslation(Translations.Error),
            message);
    }

    private string GetGitErrorDetails(GitCommandResult result)
    {
        string details = string.IsNullOrWhiteSpace(result.StandardError)
            ? result.StandardOutput
            : result.StandardError;
        details = NormalizeDiagnostic(details);

        if (string.IsNullOrWhiteSpace(details))
        {
            return string.Format(
                _context.API.GetTranslation(Translations.ErrorGitExitCode),
                result.ExitCode);
        }

        return details;
    }

    private static string NormalizeDiagnostic(string details)
    {
        details = details.Trim();
        return details.Length > MaxDiagnosticLength
            ? $"…{details[^(MaxDiagnosticLength - 1)..]}"
            : details;
    }
}
