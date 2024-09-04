using Flow.Launcher.Plugin.GitEasy.Models.Commands.Interfaces;
using Flow.Launcher.Plugin.GitEasy.Services.Interfaces;
using Flow.Launcher.Plugin.GitEasy.Utilities;
using FuzzySharp;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Flow.Launcher.Plugin.GitEasy.Models.Commands;

public class SelectCommand : ICommand
{
    public string Key => "select";
    public string Title => _context.API.GetTranslation(Translations.QueryResultSelect);
    public string Description => _context.API.GetTranslation(Translations.QueryResultSelectDesc);
    public string IconPath => Icons.Logo;

    private readonly PluginInitContext _context;
    private readonly ISettingsService _settingsService;
    private readonly IDirectoryService _directoryService;

    public SelectCommand(
        PluginInitContext context,
        ISettingsService settingsService,
        IDirectoryService directoryService
        )
    {
        _context = context;
        _settingsService = settingsService;
        _directoryService = directoryService;
    }

    public List<Result> Resolve(string query, string actionKeyword)
    {
        // Get a list of all directories under the project folder
        List<string> dirs = _directoryService.GetDirectories(_settingsService.GetSettingsOrDefault().ReposPath);

        return dirs.Select(d =>
        {
            string repoName = DirectoryUtils.ExtractRepositoryNameFromDirectory(d);
            int score = Fuzz.Ratio(d, query);
            return new Result
            {
                Title = repoName,
                SubTitle = string.Format(_context.API.GetTranslation(Translations.QueryResultSelectMsg), repoName),
                IcoPath = IconPath,
                Score = score,
                AutoCompleteText = $"{actionKeyword} {Key} {repoName}",
                Action = _ =>
                {
                    // Select the repo
                    _settingsService.GetSettings().ActiveRepo = d;
                    _context.API.ShowMsg(
                        string.Format(_context.API.GetTranslation(Translations.MsgRepoSelected), repoName),
                        string.Format(_context.API.GetTranslation(Translations.MsgRepoSelectedDesc), repoName)
                        );

                    return true;
                }
            };
        }).ToList();
    }
}
