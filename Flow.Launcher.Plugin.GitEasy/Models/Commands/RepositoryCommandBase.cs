using Flow.Launcher.Plugin.GitEasy.Models.Commands.Interfaces;
using Flow.Launcher.Plugin.GitEasy.Services.Interfaces;
using Flow.Launcher.Plugin.GitEasy.Utilities;
using FuzzySharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Flow.Launcher.Plugin.GitEasy.Models.Commands;

public abstract class RepositoryCommandBase : ICommand
{
    private readonly IDirectoryService _directoryService;

    protected RepositoryCommandBase(PluginInitContext context, IDirectoryService directoryService)
    {
        Context = context ?? throw new ArgumentNullException(nameof(context));
        _directoryService = directoryService ?? throw new ArgumentNullException(nameof(directoryService));
    }

    public abstract string Key { get; }
    public abstract string Title { get; }
    public abstract string Description { get; }
    public virtual string IconPath => Icons.Logo;

    protected PluginInitContext Context { get; }
    protected abstract string ResultMessageTranslationKey { get; }

    public async Task<List<Result>> ResolveAsync(
        string query,
        string actionKeyword,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Func<string, string, Task> executeRepositoryAction = CreateRepositoryAction();
        IReadOnlyList<string> directories = await _directoryService
            .GetRepositoriesDirectoriesAsync(cancellationToken);
        var results = new List<Result>(directories.Count);

        foreach (string directory in directories)
        {
            cancellationToken.ThrowIfCancellationRequested();
            string repositoryName = Path.GetFileName(directory);

            results.Add(new Result
            {
                Title = repositoryName,
                SubTitle = string.Format(
                    Context.API.GetTranslation(ResultMessageTranslationKey),
                    repositoryName),
                IcoPath = IconPath,
                Score = Fuzz.Ratio(directory, query),
                AutoCompleteText = !string.IsNullOrEmpty(actionKeyword)
                    ? $"{actionKeyword} {Key} {repositoryName}"
                    : $"{Key} {repositoryName}",
                AsyncAction = async _ =>
                {
                    await executeRepositoryAction(directory, repositoryName);
                    return true;
                }
            });
        }

        cancellationToken.ThrowIfCancellationRequested();
        return results;
    }

    protected abstract Func<string, string, Task> CreateRepositoryAction();
}
