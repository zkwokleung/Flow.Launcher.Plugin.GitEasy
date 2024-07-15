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
    public string Title => m_context.API.GetTranslation(Translations.QueryResultOpen);
    public string Description => m_context.API.GetTranslation(Translations.QueryResultOpenDesc);
    public string IconPath => Icons.Logo;

    private readonly PluginInitContext m_context;
    private readonly ISettingsService m_settingsService;
    private readonly IDirectoryService m_directoryService;
    private readonly ISystemCommandService m_systemCommandService;

    public OpenCommand(
        PluginInitContext context,
        ISettingsService settingsService,
        IDirectoryService directoryService,
        ISystemCommandService systemCommandService
        )
    {
        m_context = context;
        m_settingsService = settingsService;
        m_directoryService = directoryService;
        m_systemCommandService = systemCommandService;
    }

    public List<Result> Resolve(string query)
    {
        // Get a list of all directories under the project folder
        List<string> dirs = m_directoryService.GetDirectories(m_settingsService.GetSettingsOrDefault().ReposPath);

        return dirs.Select(d =>
        {
            string repoName = ExtractRepositoryNameFromDirectory(d);
            int score = Fuzz.Ratio(d, query);
            return new Result
            {
                Title = repoName,
                SubTitle = String.Format(m_context.API.GetTranslation(Translations.QueryResultOpenMsg), repoName),
                IcoPath = IconPath,
                Score = score,
                Action = _ =>
                {
                    switch (m_settingsService.GetSettingsOrDefault().OpenReposIn)
                    {
                        case OpenOption.VSCode:
                        m_systemCommandService.OpenVsCode(d);
                        break;

                        case OpenOption.FileExplorer:
                        case OpenOption.None:
                        default:
                        m_systemCommandService.OpenExplorer(d);
                        break;
                    }
                    return true;
                }
            };
        }).ToList();
    }

    #region Private Functions
    private string ExtractRepositoryNameFromDirectory(string dir)
    {
        if (String.IsNullOrWhiteSpace(dir))
        {
            return "";
        }

        int start = dir.LastIndexOf("\\", StringComparison.Ordinal);

        if (start == -1)
        {
            return dir;
        }

        return dir.Substring(start + 1);
    }
    #endregion
}
