using Flow.Launcher.Plugin.GitEasy.Models.Commands.Interfaces;
using Flow.Launcher.Plugin.GitEasy.Services.Interfaces;
using Flow.Launcher.Plugin.GitEasy.Utilities;
using FuzzySharp;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Flow.Launcher.Plugin.GitEasy.Models.Commands;

public class OpenCommand : ICommand
{
    public string Key => "open";
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
        ISystemCommandService systemCommandService
        )
    {
        _context = context;
        _settingsService = settingsService;
        _directoryService = directoryService;
        _systemCommandService = systemCommandService;
    }

    public List<Result> Resolve(string query, string actionKeyword)
    {
        // Get a list of all directories across all configured repository roots
        List<string> dirs = _directoryService.GetRepositoriesDirectories();

        return dirs.Select(d =>
        {
            string repoName = DirectoryUtils.ExtractRepositoryNameFromDirectory(d);
            int score = Fuzz.Ratio(d, query);
            return new Result
            {
                Title = repoName,
                SubTitle = string.Format(_context.API.GetTranslation(Translations.QueryResultOpenMsg), repoName),
                IcoPath = IconPath,
                Score = score,
                AutoCompleteText = $"{actionKeyword} {Key} {repoName}",
                Action = _ =>
                {
                    switch (_settingsService.GetSettingsOrDefault().OpenReposIn)
                    {
                        case OpenOption.VSCode:
                            _systemCommandService.OpenVsCode(d);
                            break;

                        case OpenOption.FileExplorer:
                        case OpenOption.None:
                        default:
                            _systemCommandService.OpenExplorer(d);
                            break;
                    }
                    return true;
                }
            };
        }).ToList();
    }
}
