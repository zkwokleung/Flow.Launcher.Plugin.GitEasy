using Flow.Launcher.Plugin.GitEasy.Models.Commands;
using Flow.Launcher.Plugin.GitEasy.Models.Commands.Interfaces;
using Flow.Launcher.Plugin.GitEasy.Services.Interfaces;
using Flow.Launcher.Plugin.GitEasy.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Flow.Launcher.Plugin.GitEasy.Services;

public class CommandService : ICommandService
{
    private Dictionary<string, ICommand> m_commands = new(StringComparer.InvariantCultureIgnoreCase);

    private PluginInitContext m_context { get; set; }

    public CommandService(IEnumerable<ICommand> commands, PluginInitContext context)
    {
        commands.ToList().ForEach(c => m_commands.Add(c.Key, c));

        m_context = context;
    }

    public async Task<List<Result>> Resolve(Query query)
    {
        List<string> args = query.Search.Split(' ').ToList();

        if (args.Count == 0)
        {
            return ShowCommands(query.ActionKeyword);
        }

        // Try to execute existing commands
        if (m_commands.TryGetValue(args[0], out ICommand result))
        {
            return result.Resolve(String.Join(" ", args.Skip(1)));
        }

        // Match possible commands
        List<Result> results = PreparePossibleCommands(query.ActionKeyword, query.Search);

        // Return the results or return the invalid result
        return results.Count == 0 ? new() { GetInvalidResult() } : results;
    }

    #region Private Functions
    private List<Result> ShowCommands(string actionKeyword)
    {
        return m_commands.Values.Select(c => PrepareCommandAutoCompleteResult(actionKeyword, c)).ToList();
    }

    private List<Result> PreparePossibleCommands(string actionKeyword, string query)
    {
        return m_commands.Values
            .Where(c => c.Key.StartsWith(query, StringComparison.InvariantCultureIgnoreCase))
            .Select(c => PrepareCommandAutoCompleteResult(actionKeyword, c)).ToList();
    }

    private Result PrepareCommandAutoCompleteResult(string actionKeyword, ICommand command)
    {
        return new Result
        {
            Title = command.Title,
            SubTitle = command.Description,
            IcoPath = command.IconPath ?? Icons.Logo,
            Action = _ =>
            {
                m_context.API.ChangeQuery($"{actionKeyword} {command.Key}");
                return false;
            }
        };
    }

    private Result GetInvalidResult()
    {
        return new Result
        {
            Title = m_context.API.GetTranslation(Translations.QueryInvalidCmd),
            SubTitle = m_context.API.GetTranslation(Translations.QueryInvalidCmdMsg)
        };
    }
    #endregion
}
