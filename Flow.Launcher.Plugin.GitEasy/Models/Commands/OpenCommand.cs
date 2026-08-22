using Flow.Launcher.Plugin.GitEasy.Models.Commands.Interfaces;
using Flow.Launcher.Plugin.GitEasy.Services.Interfaces;
using Flow.Launcher.Plugin.GitEasy.Utilities;
using FuzzySharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Flow.Launcher.Plugin.GitEasy.Models.Commands;

public class OpenCommand : ICommand
{
    private const int MaxDiagnosticLength = 1000;

    public string Key => "Open";
    public string Title => _context.API.GetTranslation(Translations.QueryResultOpen);
    public string Description => _context.API.GetTranslation(Translations.QueryResultOpenDesc);
    public string IconPath => Icons.Logo;

    private readonly PluginInitContext _context;
    private readonly ISettingsService _settingsService;
    private readonly IDirectoryService _directoryService;
    private readonly ISystemCommandService _systemCommandService;

    public OpenCommand(
        PluginInitContext context,
        ISettingsService settingsService,
        IDirectoryService directoryService,
        ISystemCommandService systemCommandService)
    {
        _context = context;
        _settingsService = settingsService;
        _directoryService = directoryService;
        _systemCommandService = systemCommandService;
    }

    public List<Result> Resolve(string query, string actionKeyword)
    {
        OpenOption openOption = _settingsService.GetSettingsOrDefault().OpenReposIn;
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
                    await OpenRepositoryAsync(directory, openOption);
                    return true;
                }
            };
        }).ToList();
    }

    private async Task OpenRepositoryAsync(string repositoryPath, OpenOption openOption)
    {
        try
        {
            switch (openOption)
            {
                case OpenOption.VSCode:
                    await _systemCommandService.OpenVsCodeAsync(repositoryPath);
                    break;

                case OpenOption.Cursor:
                    await _systemCommandService.OpenCursorAsync(repositoryPath);
                    break;

                case OpenOption.FileExplorer:
                case OpenOption.None:
                default:
                    await _systemCommandService.OpenExplorerAsync(repositoryPath);
                    break;
            }
        }
        catch (Exception exception)
        {
            ShowOpenRepositoryError(repositoryPath, exception);
        }
    }

    private void ShowOpenRepositoryError(string repositoryPath, Exception exception)
    {
        string message = string.Format(
            _context.API.GetTranslation(Translations.ErrorOpenRepository),
            repositoryPath);
        string details = NormalizeDiagnostic(exception.Message);

        if (!string.IsNullOrWhiteSpace(details))
        {
            message += $"{Environment.NewLine}{details}";
        }

        _context.API.ShowMsgError(
            _context.API.GetTranslation(Translations.Error),
            message);
    }

    private static string NormalizeDiagnostic(string details)
    {
        details = details.Trim();
        return details.Length > MaxDiagnosticLength
            ? $"…{details[^(MaxDiagnosticLength - 1)..]}"
            : details;
    }
}
