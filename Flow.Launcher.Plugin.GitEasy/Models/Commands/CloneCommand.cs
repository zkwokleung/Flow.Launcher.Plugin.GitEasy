using Flow.Launcher.Plugin.GitEasy.Models.Commands.Interfaces;
using Flow.Launcher.Plugin.GitEasy.Models.Commands.Results;
using Flow.Launcher.Plugin.GitEasy.Services.Interfaces;
using Flow.Launcher.Plugin.GitEasy.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Flow.Launcher.Plugin.GitEasy.Models.Commands;

public class CloneCommand : ICommand
{
    private const int MaxDiagnosticLength = 1000;

    public string Key => "Clone";
    public string Title => _context.API.GetTranslation(Translations.QueryResultClone);
    public string Description => _context.API.GetTranslation(Translations.QueryResultCloneDesc);

    private readonly PluginInitContext _context;
    private readonly IGitCommandService _gitCommandService;
    private readonly ISettingsService _settingsService;
    private readonly IDirectoryService _directoryService;
    private readonly ISystemCommandService _systemCommandService;

    public CloneCommand(
        PluginInitContext context,
        ISettingsService settingsService,
        IDirectoryService directoryService,
        IGitCommandService gitCommandService,
        ISystemCommandService systemCommandService)
    {
        _context = context;
        _settingsService = settingsService;
        _directoryService = directoryService;
        _gitCommandService = gitCommandService;
        _systemCommandService = systemCommandService;
    }

    public async Task<List<Result>> ResolveAsync(
        string query,
        string actionKeyword,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(query))
        {
            // Display a hint result
            return CompleteResolution(new()
            {
                new Result
                {
                    Title = _context.API.GetTranslation(Translations.QueryResultCloneHint),
                    IcoPath = Icons.Logo,
                    Action = _ => true,
                }
            }, cancellationToken);
        }

        List<string> terms = query
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToList();
        List<string> repositories = terms.Where(term => TryExtractRepositoryName(term, out _)).ToList();

        if (repositories.Count == 0)
        {
            return CompleteResolution(new()
            {
                new Result
                {
                    Title = _context.API.GetTranslation(Translations.QueryResultCloneNoRepos),
                    IcoPath = Icons.Logo,
                    Action = _ => true
                }
            }, cancellationToken);
        }

        if (repositories.Count != 1)
        {
            return CompleteResolution(GetInvalidCloneResults(), cancellationToken);
        }

        string repository = repositories[0];
        terms.RemoveAt(terms.IndexOf(repository));

        if (!TryParseCloneArguments(terms, out IReadOnlyList<string> cloneArguments)
            || !TryExtractRepositoryName(repository, out string location))
        {
            return CompleteResolution(GetInvalidCloneResults(), cancellationToken);
        }

        var settings = _settingsService.GetSettingsOrDefault();
        OpenOption defaultPostAction = settings.OpenReposIn;
        IReadOnlyList<string> repoRoots = await _directoryService
            .GetExistingRepositoryRootsAsync(cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();

        List<Result> results = new();

        foreach (string root in repoRoots)
        {
            cancellationToken.ThrowIfCancellationRequested();
            string destinationPath = Path.Combine(root, location);

            // Default clone (follow OpenReposIn setting)
            results.Add(new Result
            {
                Title = $"{_context.API.GetTranslation(Translations.QueryResultClone)} {location} → {root}",
                SubTitle = string.Format(_context.API.GetTranslation(Translations.QueryResultCloneMsg), repository, root),
                IcoPath = Icons.Logo,
                AsyncAction = async _ =>
                {
                    await ExecuteCloneAsync(repository, cloneArguments, destinationPath, location, defaultPostAction);
                    return true;
                }
            });

            // Clone and open Explorer
            results.Add(new Result
            {
                Title = $"{_context.API.GetTranslation(Translations.QueryResultCloneOpenExplorer)} ({root})",
                IcoPath = Icons.Explorer,
                AsyncAction = async _ =>
                {
                    await ExecuteCloneAsync(repository, cloneArguments, destinationPath, location, OpenOption.FileExplorer);
                    return true;
                }
            });

            // Clone and open VSCode
            results.Add(new Result
            {
                Title = $"{_context.API.GetTranslation(Translations.QueryResultCloneOpenVSCode)} ({root})",
                IcoPath = Icons.VSCode,
                AsyncAction = async _ =>
                {
                    await ExecuteCloneAsync(repository, cloneArguments, destinationPath, location, OpenOption.VSCode);
                    return true;
                }
            });

            // Clone and open in Cursor
            results.Add(new Result
            {
                Title = $"{_context.API.GetTranslation(Translations.QueryResultCloneOpenCursor)} ({root})",
                IcoPath = Icons.Cursor,
                AsyncAction = async _ =>
                {
                    await ExecuteCloneAsync(repository, cloneArguments, destinationPath, location, OpenOption.Cursor);
                    return true;
                }
            });
        }

        return CompleteResolution(results, cancellationToken);
    }

    private static List<Result> CompleteResolution(
        List<Result> results,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return results;
    }

    private async Task ExecuteCloneAsync(
        string repositoryUrl,
        IReadOnlyList<string> arguments,
        string destinationPath,
        string location,
        OpenOption postAction)
    {
        try
        {
            GitCommandResult result = await _gitCommandService.CloneRepositoryAsync(
                new()
                {
                    Arguments = arguments,
                    DestinationPath = destinationPath,
                    Repo = repositoryUrl
                },
                CancellationToken.None);

            if (!result.Succeeded)
            {
                ShowCloneError(result);
                return;
            }

            ShowCloneCompleteMsg(location);
            await OpenRepositoryAsync(destinationPath, postAction);
        }
        catch (TimeoutException)
        {
            _context.API.ShowMsgError(
                _context.API.GetTranslation(Translations.Error),
                string.Format(
                    _context.API.GetTranslation(Translations.ErrorCloneTimeout),
                    destinationPath));
        }
        catch (Exception ex)
        {
            _context.API.ShowMsgError(
                _context.API.GetTranslation(Translations.Error),
                NormalizeDiagnostic(ex.Message));
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
        details = NormalizeDiagnostic(details);

        if (string.IsNullOrWhiteSpace(details))
        {
            details = string.Format(
                _context.API.GetTranslation(Translations.ErrorGitExitCode),
                result.ExitCode);
        }

        _context.API.ShowMsgError(
            _context.API.GetTranslation(Translations.Error),
            details);
    }

    private async Task OpenRepositoryAsync(string destinationPath, OpenOption postAction)
    {
        try
        {
            switch (postAction)
            {
                case OpenOption.FileExplorer:
                    await _systemCommandService.OpenExplorerAsync(destinationPath);
                    break;
                case OpenOption.VSCode:
                    await _systemCommandService.OpenVsCodeAsync(destinationPath);
                    break;
                case OpenOption.Cursor:
                    await _systemCommandService.OpenCursorAsync(destinationPath);
                    break;
            }
        }
        catch (Exception exception)
        {
            ShowOpenRepositoryError(destinationPath, exception);
        }
    }

    private void ShowOpenRepositoryError(string destinationPath, Exception exception)
    {
        string message = string.Format(
            _context.API.GetTranslation(Translations.ErrorOpenRepository),
            destinationPath);
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
