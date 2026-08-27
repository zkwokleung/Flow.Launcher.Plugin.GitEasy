using Flow.Launcher.Plugin.GitEasy.Models.Commands.Interfaces;
using Flow.Launcher.Plugin.GitEasy.Services.Interfaces;
using Flow.Launcher.Plugin.GitEasy.Utilities;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Flow.Launcher.Plugin.GitEasy.Services;

public class CommandService : ICommandService
{
    private readonly Dictionary<string, ICommand> _commands = new(StringComparer.OrdinalIgnoreCase);
    private readonly PluginInitContext _context;

    public CommandService(IEnumerable<ICommand> commands, PluginInitContext context)
    {
        foreach (ICommand c in commands)
        {
            _commands.Add(c.Key, c);
        }

        _context = context;
    }

    public async Task<List<Result>> ResolveAsync(
        Query query,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        string search = query.Search ?? string.Empty;
        string[] args = search.Split(
            ' ',
            StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (args.Length == 0)
        {
            return GetCommandCompletionResults(
                query.ActionKeyword,
                string.Empty,
                cancellationToken);
        }

        if (_commands.TryGetValue(args[0], out ICommand result))
        {
            string commandQuery = args.Length > 1
                ? string.Join(" ", args, 1, args.Length - 1)
                : string.Empty;
            List<Result> commandResults = await result.ResolveAsync(
                commandQuery,
                query.ActionKeyword,
                cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            return commandResults;
        }

        List<Result> results = GetCommandCompletionResults(
            query.ActionKeyword,
            search,
            cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();
        return results.Count == 0 ? new() { GetInvalidResult() } : results;
    }

    private List<Result> GetCommandCompletionResults(
        string actionKeyword,
        string query,
        CancellationToken cancellationToken)
    {
        var results = new List<Result>(_commands.Count);

        foreach (ICommand c in _commands.Values)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (string.IsNullOrEmpty(query)
                || c.Key.StartsWith(query, StringComparison.OrdinalIgnoreCase))
            {
                results.Add(PrepareCommandAutoCompleteResult(actionKeyword, c));
            }
        }

        cancellationToken.ThrowIfCancellationRequested();
        return results;
    }

    private Result PrepareCommandAutoCompleteResult(string actionKeyword, ICommand command)
    {
        string commandCompletion =
            (!string.IsNullOrEmpty(actionKeyword) ? $"{actionKeyword} " : string.Empty)
            + $"{command.Key} ";

        return new Result
        {
            Title = command.Title,
            SubTitle = command.Description,
            IcoPath = command.IconPath ?? Icons.Logo,
            AutoCompleteText = commandCompletion,
            Action = _ =>
            {
                _context.API.ChangeQuery(commandCompletion);
                return false;
            }
        };
    }

    private Result GetInvalidResult()
    {
        return new Result
        {
            Title = _context.API.GetTranslation(Translations.ErrorInvalidCmd),
            SubTitle = _context.API.GetTranslation(Translations.ErrorInvalidCmdMsg),
            IcoPath = Icons.Error
        };
    }
}
