using Flow.Launcher.Plugin.GitEasy.Models.Commands.Interfaces;
using Flow.Launcher.Plugin.GitEasy.Services.Interfaces;
using Flow.Launcher.Plugin.GitEasy.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Flow.Launcher.Plugin.GitEasy.Models.Commands;

public class CloneCommand : ICommand
{
    public string Key => "clone";
    public string Title => m_context.API.GetTranslation(Translations.QueryResultClone);
    public string Description => m_context.API.GetTranslation(Translations.QueryResultCloneDesc);

    private PluginInitContext m_context;
    private IGitCommandService m_gitCommandService;
    private ISettingsService m_settingsService;
    private ISystemCommandService m_systemCommandService;

    public CloneCommand(
        PluginInitContext context,
        ISettingsService settingsService,
        IGitCommandService gitCommandService,
        ISystemCommandService systemCommandService
        )
    {
        m_context = context;
        m_settingsService = settingsService;
        m_gitCommandService = gitCommandService;
        m_systemCommandService = systemCommandService;
    }

    public List<Result> Resolve(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return new();
        }

        List<string> terms = query.Split(' ').ToList();

        // Use regex to indentify the repo for the flexibility
        List<string> repos = terms.Where(t => RegexUtils.ReposRegex().IsMatch(t)).ToList();

        if (repos.Count < 1)
        {
            return new()
            {
                new Result{
                    Title= m_context.API.GetTranslation(Translations.QueryResultCloneNoRepos),
                    IcoPath = Icons.Logo,
                    Action = a => { return true; }
                }
            };
        }

        string firstRepos = repos.First();
        string options = string.Join(" ", terms.Remove(firstRepos));

        // Extract the repos folder name from the repos url
        string location = ExtractRepoName(firstRepos);

        return new()
        {
            new Result{
                Title= $"{m_context.API.GetTranslation(Translations.QueryResultClone)} {location}",
                SubTitle =  string.Format(m_context.API.GetTranslation(Translations.QueryResultCloneMsg), firstRepos, m_settingsService.GetSettingsOrDefault().ReposPath),
                IcoPath = Icons.Logo,
                Action = a =>
                {
                    try
                    {
                        m_gitCommandService.CloneRepos(new()
                        {
                            Options = options,
                            DestinationFolder = m_settingsService.GetSettingsOrDefault().ReposPath,
                            Repo = firstRepos
                        }, () =>
                        {
                            m_context.API.ShowMsg(
                                m_context.API.GetTranslation(Translations.QueryCloneComplete),
                                $"{m_context.API.GetTranslation(Translations.QueryClonseCompleteMsg)} {location}"
                            );
                            

                            // Open explorer/vscode after execution
                            switch(m_settingsService.GetSettingsOrDefault().OpenReposIn)
                            {
                                case OpenOption.FileExplorer:
                                    m_systemCommandService.OpenExplorer($"{m_settingsService.GetSettingsOrDefault().ReposPath}\\{location}");
                                    break;

                                case OpenOption.VSCode:
                                    m_systemCommandService.OpenVsCode($"{m_settingsService.GetSettingsOrDefault().ReposPath}\\{location}");
                                    break;

                                default:
                                    break;
                            }
                        });
                    }catch (Exception ex)
                    {
                        m_context.API.ShowMsgError("Error", ex.Message);
                    }
                    return true;
                }
            }
        };
    }

    #region Private Functions
    private string ExtractRepoName(string url)
    {
        // Look for the last index of the last slash /
        int start = url.LastIndexOf('/') + 1;
        return url.Substring(start, url.Length - start - 4);
    }
    #endregion
}