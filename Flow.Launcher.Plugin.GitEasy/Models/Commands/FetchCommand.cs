using Flow.Launcher.Plugin.GitEasy.Models.Commands.Interfaces;
using Flow.Launcher.Plugin.GitEasy.Services.Interfaces;
using Flow.Launcher.Plugin.GitEasy.Utilities;
using FuzzySharp;
using System.Collections.Generic;
using System.Linq;

namespace Flow.Launcher.Plugin.GitEasy.Models.Commands;

public class FetchCommand : ICommand
{
    public string Key => "fetch";
    public string Title => _context.API.GetTranslation(Translations.QueryResultFetch);
    public string Description => _context.API.GetTranslation(Translations.QueryResultFetchDesc);
    public string IconPath => Icons.Logo;

    private readonly PluginInitContext _context;
    private readonly ISettingsService _settingsService;
    private readonly IGitCommandService _gitCommandService;
    private readonly IDirectoryService _directoryService;

    public FetchCommand(
        PluginInitContext context,
        ISettingsService settingsService,
        IGitCommandService gitCommandService,
        IDirectoryService directoryService)
    {
        _context = context;
        _settingsService = settingsService;
        _gitCommandService = gitCommandService;
        _directoryService = directoryService;
    }

    public List<Result> Resolve(string query)
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
                SubTitle = string.Format(_context.API.GetTranslation(Translations.QueryResultOpenMsg), repoName),
                IcoPath = IconPath,
                Score = score,
                Action = _ =>
                {
                    _gitCommandService.FetchRepos(new()
                    {
                        RepoPath = d
                    },
                    args =>
                    {
                        _context.API.ShowMsg(args.Output, iconPath: Icons.Logo);
                    });

                    return true;
                }
            };
        }).ToList();
    }
}
