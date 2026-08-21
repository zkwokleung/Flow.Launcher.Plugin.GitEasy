using Flow.Launcher.Plugin.GitEasy.Models.Commands.Interfaces;
using Flow.Launcher.Plugin.GitEasy.Models.Commands.Results;
using Flow.Launcher.Plugin.GitEasy.Services.Interfaces;
using Flow.Launcher.Plugin.GitEasy.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Flow.Launcher.Plugin.GitEasy.Models.Commands;

public class CloneCommand : ICommand
{
    public string Key => "Clone";
    public string Title => _context.API.GetTranslation(Translations.QueryResultClone);
    public string Description => _context.API.GetTranslation(Translations.QueryResultCloneDesc);

    private readonly PluginInitContext _context;
    private readonly IGitCommandService _gitCommandService;
    private readonly ISettingsService _settingsService;
    private readonly ISystemCommandService _systemCommandService;

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

        List<string> terms = query
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToList();
        List<string> repositories = terms.Where(term => TryExtractRepositoryName(term, out _)).ToList();

        if (repositories.Count == 0)
        {
            return new()
            {
                new Result
                {
                    Title = _context.API.GetTranslation(Translations.QueryResultCloneNoRepos),
                    IcoPath = Icons.Logo,
                    Action = _ => true
                }
            };
        }

        if (repositories.Count != 1)
        {
            return GetInvalidCloneResults();
        }

        string repository = repositories[0];
        terms.RemoveAt(terms.IndexOf(repository));

        if (!TryParseCloneArguments(terms, out IReadOnlyList<string> cloneArguments)
            || !TryExtractRepositoryName(repository, out string location))
        {
            return GetInvalidCloneResults();
        }

        var settings = _settingsService.GetSettingsOrDefault();
        OpenOption defaultPostAction = settings.OpenReposIn;
        List<string> repoRoots = (settings.ReposPaths ?? new())
            .Where(root => !string.IsNullOrWhiteSpace(root) && Directory.Exists(root))
            .Select(Path.GetFullPath)
            .Select(Path.TrimEndingDirectorySeparator)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        List<Result> results = new();

        foreach (string root in repoRoots)
        {
            string destinationPath = Path.Combine(root, location);

            // Default clone (follow OpenReposIn setting)
            results.Add(new Result
            {
                Title = $"{_context.API.GetTranslation(Translations.QueryResultClone)} {location} → {root}",
                SubTitle = string.Format(_context.API.GetTranslation(Translations.QueryResultCloneMsg), repository, root),
                IcoPath = Icons.Logo,
                Action = _ =>
                {
                    ExecuteClone(repository, cloneArguments, destinationPath, location, defaultPostAction);
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
                    ExecuteClone(repository, cloneArguments, destinationPath, location, OpenOption.FileExplorer);
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
                    ExecuteClone(repository, cloneArguments, destinationPath, location, OpenOption.VSCode);
                    return true;
                }
            });

            // Clone and open in Cursor
            results.Add(new Result
            {
                Title = $"{_context.API.GetTranslation(Translations.QueryResultCloneOpenCursor)} ({root})",
                IcoPath = Icons.Cursor,
                Action = _ =>
                {
                    ExecuteClone(repository, cloneArguments, destinationPath, location, OpenOption.Cursor);
                    return true;
                }
            });
        }

        return results;
    }

    private void ExecuteClone(
        string repositoryUrl,
        IReadOnlyList<string> arguments,
        string destinationPath,
        string location,
        OpenOption postAction)
    {
        try
        {
            GitCommandResult result = _gitCommandService.CloneRepos(new()
            {
                Arguments = arguments,
                DestinationPath = destinationPath,
                Repo = repositoryUrl
            });

            if (!result.Succeeded)
            {
                ShowCloneError(result);
                return;
            }

            ShowCloneCompleteMsg(location);
            OpenRepository(destinationPath, postAction);
        }
        catch (Exception ex)
        {
            _context.API.ShowMsgError(
                _context.API.GetTranslation(Translations.Error),
                ex.Message);
        }
    }

    private void ShowCloneCompleteMsg(string location)
    {
        _context.API.ShowMsg(
            _context.API.GetTranslation(Translations.QueryCloneComplete),
            $"{_context.API.GetTranslation(Translations.QueryClonseCompleteMsg)} {location}");
    }

    private void ShowCloneError(GitCommandResult result)
    {
        string details = string.IsNullOrWhiteSpace(result.StandardError)
            ? result.StandardOutput
            : result.StandardError;
        details = details.Trim();

        if (string.IsNullOrWhiteSpace(details))
        {
            details = $"Git exited with code {result.ExitCode}.";
        }
        else if (details.Length > 1000)
        {
            details = $"…{details[^1000..]}";
        }

        _context.API.ShowMsgError(
            _context.API.GetTranslation(Translations.Error),
            details);
    }

    private void OpenRepository(string destinationPath, OpenOption postAction)
    {
        switch (postAction)
        {
            case OpenOption.FileExplorer:
                _systemCommandService.OpenExplorer(destinationPath);
                break;
            case OpenOption.VSCode:
                _systemCommandService.OpenVsCode(destinationPath);
                break;
            case OpenOption.Cursor:
                _systemCommandService.OpenCursor(destinationPath);
                break;
        }
    }

    private List<Result> GetInvalidCloneResults()
    {
        return new()
        {
            new Result
            {
                Title = _context.API.GetTranslation(Translations.ErrorInvalidCmd),
                SubTitle = _context.API.GetTranslation(Translations.ErrorInvalidCmdMsg),
                IcoPath = Icons.Error,
                Action = _ => true
            }
        };
    }

    private static bool TryParseCloneArguments(
        IReadOnlyList<string> terms,
        out IReadOnlyList<string> arguments)
    {
        List<string> parsedArguments = new();

        for (int index = 0; index < terms.Count; index++)
        {
            string term = terms[index];

            switch (term)
            {
                case "--branch":
                case "-b":
                    if (++index >= terms.Count
                        || string.IsNullOrWhiteSpace(terms[index])
                        || terms[index].StartsWith("-", StringComparison.Ordinal))
                    {
                        arguments = Array.Empty<string>();
                        return false;
                    }

                    parsedArguments.Add(term);
                    parsedArguments.Add(terms[index]);
                    break;

                case "--depth":
                    if (++index >= terms.Count
                        || !int.TryParse(terms[index], out int depth)
                        || depth <= 0)
                    {
                        arguments = Array.Empty<string>();
                        return false;
                    }

                    parsedArguments.Add(term);
                    parsedArguments.Add(terms[index]);
                    break;

                case "--recurse-submodules":
                case "--single-branch":
                    parsedArguments.Add(term);
                    break;

                default:
                    arguments = Array.Empty<string>();
                    return false;
            }
        }

        arguments = parsedArguments.ToArray();
        return true;
    }

    private static bool TryExtractRepositoryName(string repositoryUrl, out string repositoryName)
    {
        string repositoryPath;

        if (Uri.TryCreate(repositoryUrl, UriKind.Absolute, out Uri repositoryUri)
            && repositoryUri.Scheme.Equals(Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
        {
            repositoryPath = Uri.UnescapeDataString(repositoryUri.AbsolutePath);
        }
        else
        {
            int separatorIndex = repositoryUrl.IndexOf(':');

            if (!repositoryUrl.StartsWith("git@", StringComparison.Ordinal)
                || separatorIndex <= "git@".Length
                || separatorIndex == repositoryUrl.Length - 1)
            {
                repositoryName = string.Empty;
                return false;
            }

            string host = repositoryUrl["git@".Length..separatorIndex];
            if (host.Any(char.IsWhiteSpace))
            {
                repositoryName = string.Empty;
                return false;
            }

            repositoryPath = repositoryUrl[(separatorIndex + 1)..];
        }

        repositoryPath = repositoryPath.TrimEnd('/');
        int lastSeparatorIndex = repositoryPath.LastIndexOf('/');
        repositoryName = repositoryPath[(lastSeparatorIndex + 1)..];

        if (repositoryName.EndsWith(".git", StringComparison.OrdinalIgnoreCase))
        {
            repositoryName = repositoryName[..^4];
        }

        return !string.IsNullOrWhiteSpace(repositoryName)
            && repositoryName is not "." and not ".."
            && repositoryName.IndexOfAny(Path.GetInvalidFileNameChars()) < 0;
    }
}
