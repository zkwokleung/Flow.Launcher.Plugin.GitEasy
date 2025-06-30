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

        // Determine repository root paths
        List<string> repoRoots = _settingsService.GetSettingsOrDefault().ReposPaths;
        if (repoRoots == null || repoRoots.Count == 0)
        {
            repoRoots = new() { _settingsService.GetSettingsOrDefault().ReposPath };
        }

        List<Result> results = new();

        foreach (string root in repoRoots)
        {
            if (string.IsNullOrWhiteSpace(root))
            {
                continue;
            }

            string destinationPath = $"{root}\\{location}";

            // Default clone (follow OpenReposIn setting)
            results.Add(new Result
            {
                Title = $"{_context.API.GetTranslation(Translations.QueryResultClone)} {location} → {root}",
                SubTitle = string.Format(_context.API.GetTranslation(Translations.QueryResultCloneMsg), firstRepos, root),
                IcoPath = Icons.Logo,
                Action = _ =>
                {
                    ExecuteClone(firstRepos, options, root, location, _settingsService.GetSettingsOrDefault().OpenReposIn);
                    return true;
                }
            });

            // Clone and open Explorer
            results.Add(new Result
            {
                Title = $"{_context.API.GetTranslation(Translations.QueryResultCloneOpenExplorer)} ({root})",
                IcoPath = Icons.Explorer,
                Action = _ =>
                {
                    ExecuteClone(firstRepos, options, root, location, OpenOption.FileExplorer);
                    return true;
                }
            });

            // Clone and open VSCode
            results.Add(new Result
            {
                Title = $"{_context.API.GetTranslation(Translations.QueryResultCloneOpenVSCode)} ({root})",
                IcoPath = Icons.VSCode,
                Action = _ =>
                {
                    ExecuteClone(firstRepos, options, root, location, OpenOption.VSCode);
                    return true;
                }
            });
        }

        return results;
    }

    private void ExecuteClone(string repoUrl, string options, string rootPath, string location, OpenOption postAction)
    {
        try
        {
            _gitCommandService.CloneRepos(new()
            {
                Options = options,
                DestinationFolder = rootPath,
                Repo = repoUrl
            }, () =>
            {
                ShowCloneCompleteMsg(location);

                switch (postAction)
                {
                    case OpenOption.FileExplorer:
                        _systemCommandService.OpenExplorer($"{rootPath}\\{location}");
                        break;
                    case OpenOption.VSCode:
                        _systemCommandService.OpenVsCode($"{rootPath}\\{location}");
                        break;
                    default:
                        break;
                }
            });
        }
        catch (Exception ex)
        {
            _context.API.ShowMsgError("Error", ex.Message);
        }
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