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
    public string Title => _context.API.GetTranslation(Translations.QueryResultClone);
    public string Description => _context.API.GetTranslation(Translations.QueryResultCloneDesc);

    private PluginInitContext _context;
    private IGitCommandService _gitCommandService;
    private ISettingsService _settingsService;
    private ISystemCommandService _systemCommandService;

    public CloneCommand(
        PluginInitContext context,
        ISettingsService settingsService,
        IGitCommandService gitCommandService,
        ISystemCommandService systemCommandService
        )
    {
        _context = context;
        _settingsService = settingsService;
        _gitCommandService = gitCommandService;
        _systemCommandService = systemCommandService;
    }

    public List<Result> Resolve(string query, string actionKeyword)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            // Display a hint result
            return new()
            {
                new Result
                {
                    Title = _context.API.GetTranslation(Translations.QueryResultCloneHint),
                    IcoPath = Icons.Logo,
                    Action = _ => true,
                }
            };
        }

        List<string> terms = query.Split(' ').ToList();

        // Use regex to indentify the repo for the flexibility
        List<string> repos = terms.Where(t => RegexUtils.ReposRegex().IsMatch(t)).ToList();

        if (repos.Count < 1)
        {
            return new()
            {
                new Result{
                    Title= _context.API.GetTranslation(Translations.QueryResultCloneNoRepos),
                    IcoPath = Icons.Logo,
                    Action = _ => true,
                }
            };
        }

        string firstRepos = repos.First();
        string options = string.Join(" ", terms.Remove(firstRepos));

        // Extract the repos folder name from the repos url
        string location = ExtractRepoName(firstRepos);

        return new()
        {
            // Default option
            new Result {
                Title= $"{_context.API.GetTranslation(Translations.QueryResultClone)} {location}",
                SubTitle =  string.Format(_context.API.GetTranslation(Translations.QueryResultCloneMsg), firstRepos, _settingsService.GetSettingsOrDefault().ReposPath),
                IcoPath = Icons.Logo,
                Action = _ =>
                {
                    try
                    {
                        _gitCommandService.CloneRepos(new()
                        {
                            Options = options,
                            DestinationFolder = _settingsService.GetSettingsOrDefault().ReposPath,
                            Repo = firstRepos
                        }, () =>
                        {
                            ShowCloneCompleteMsg(location);
                            
                            // Open explorer/vscode after execution
                            switch(_settingsService.GetSettingsOrDefault().OpenReposIn)
                            {
                                case OpenOption.FileExplorer:
                                    _systemCommandService.OpenExplorer($"{_settingsService.GetSettingsOrDefault().ReposPath}\\{location}");
                                    break;

                                case OpenOption.VSCode:
                                    _systemCommandService.OpenVsCode($"{_settingsService.GetSettingsOrDefault().ReposPath}\\{location}");
                                    break;

                                default:
                                    break;
                            }
                        });
                    }catch (Exception ex)
                    {
                        _context.API.ShowMsgError("Error", ex.Message);
                    }
                    return true;
                }
            },

            // Clone and open in Explorer
            new Result
            {
                Title= _context.API.GetTranslation(Translations.QueryResultCloneOpenExplorer),
                IcoPath = Icons.Explorer,
                Action = _ =>
                {
                    try
                    {
                        _gitCommandService.CloneRepos(new()
                        {
                            Options = options,
                            DestinationFolder = _settingsService.GetSettingsOrDefault().ReposPath,
                            Repo = firstRepos
                        }, () =>
                        {
                            ShowCloneCompleteMsg(location);
                            _systemCommandService.OpenExplorer($"{_settingsService.GetSettingsOrDefault().ReposPath}\\{location}");
                        });
                    }catch (Exception ex)
                    {
                        _context.API.ShowMsgError("Error", ex.Message);
                    }
                    return true;
                }
            },

            // Clone and open in VS Code
            new Result
            {
                Title= _context.API.GetTranslation(Translations.QueryResultCloneOpenVSCode),
                IcoPath = Icons.VSCode,
                Action = _ =>
                {
                    try
                    {
                        _gitCommandService.CloneRepos(new()
                        {
                            Options = options,
                            DestinationFolder = _settingsService.GetSettingsOrDefault().ReposPath,
                            Repo = firstRepos
                        }, () =>
                        {
                            ShowCloneCompleteMsg(location);
                            _systemCommandService.OpenVsCode($"{_settingsService.GetSettingsOrDefault().ReposPath}\\{location}");
                        });
                    }catch (Exception ex)
                    {
                        _context.API.ShowMsgError("Error", ex.Message);
                    }
                    return true;
                }
            }
        };
    }

    #region Private Functions
    private void ShowCloneCompleteMsg(string location)
    {
        _context.API.ShowMsg(
            _context.API.GetTranslation(Translations.QueryCloneComplete),
            $"{_context.API.GetTranslation(Translations.QueryClonseCompleteMsg)} {location}"
            );
    }

    private string ExtractRepoName(string url)
    {
        // Look for the last index of the last slash /
        int start = url.LastIndexOf('/') + 1;
        return url.Substring(start, url.Length - start - 4);
    }
    #endregion
}