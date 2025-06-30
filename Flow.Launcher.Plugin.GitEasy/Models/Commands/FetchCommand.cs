using Flow.Launcher.Plugin.GitEasy.Models.Commands.Interfaces;
using Flow.Launcher.Plugin.GitEasy.Models.Commands.Options;
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
                    _gitCommandService.FetchRepos(
                        new GitFetchCommandOptions()
                        {
                            RepoPath = d
                        },
                        e =>
                        {
                            if (e.ExitCode == 0)
                            {
                                _context.API.ShowMsg(
                                    _context.API.GetTranslation(Translations.QueryFetchComplete),
                                    string.Format(_context.API.GetTranslation(Translations.QueryFetchCompleteMsg), repoName),
                                    iconPath: IconPath
                                );
                            }
                            else
                            {
                                _context.API.ShowMsgError(
                                    _context.API.GetTranslation(Translations.Error),
                                    string.Format(_context.API.GetTranslation(Translations.ErrorFetchMsg), repoName)
                                );
                            }
                        }
                    );

                    return true;
                }
            };
        }).ToList();
    }
}
