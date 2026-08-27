using Flow.Launcher.Plugin.GitEasy.Models.Commands.Results;
using Flow.Launcher.Plugin.GitEasy.Services.Interfaces;
using Flow.Launcher.Plugin.GitEasy.Utilities;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Flow.Launcher.Plugin.GitEasy.Models.Commands;

public class FetchCommand : RepositoryCommandBase
{
    public override string Key => "Fetch";
    public override string Title => Context.API.GetTranslation(Translations.QueryResultFetch);
    public override string Description => Context.API.GetTranslation(Translations.QueryResultFetchDesc);
    public override string IconPath => Icons.Logo;

    private readonly IGitCommandService _gitCommandService;

    public FetchCommand(
        PluginInitContext context,
        IGitCommandService gitCommandService,
        IDirectoryService directoryService)
        : base(context, directoryService)
    {
        _gitCommandService = gitCommandService;
    }

    protected override string ResultMessageTranslationKey => Translations.QueryResultFetchMsg;

    protected override Func<string, string, Task> CreateRepositoryAction()
    {
        return StartFetchInBackground;
    }

    private Task StartFetchInBackground(string repositoryPath, string repositoryName)
    {
        Context.API.ShowMsg(
            Context.API.GetTranslation(Translations.QueryFetchStarted),
            string.Format(
                Context.API.GetTranslation(Translations.QueryFetchStartedMsg),
                repositoryName),
            iconPath: IconPath);

        _ = ExecuteFetchAsync(repositoryPath, repositoryName);
        return Task.CompletedTask;
    }

    private async Task ExecuteFetchAsync(string repositoryPath, string repositoryName)
    {
        try
        {
            GitCommandResult result = await _gitCommandService.FetchRepositoryAsync(
                repositoryPath,
                CancellationToken.None);

            if (!result.Succeeded)
            {
                ShowFetchError(
                    repositoryName,
                    CommandErrorFormatter.GetGitFailureDetails(
                        result,
                        Context.API.GetTranslation(Translations.ErrorGitExitCode)));
                return;
            }

            Context.API.ShowMsg(
                Context.API.GetTranslation(Translations.QueryFetchComplete),
                string.Format(
                    Context.API.GetTranslation(Translations.QueryFetchCompleteMsg),
                    repositoryName),
                iconPath: IconPath);
        }
        catch (TimeoutException)
        {
            Context.API.ShowMsgError(
                Context.API.GetTranslation(Translations.Error),
                string.Format(
                    Context.API.GetTranslation(Translations.ErrorFetchTimeout),
                    repositoryName));
        }
        catch (Exception exception)
        {
            ShowFetchError(repositoryName, exception.Message);
        }
    }

    private void ShowFetchError(string repositoryName, string details)
    {
        string message = CommandErrorFormatter.FormatWithDetails(
            string.Format(
                Context.API.GetTranslation(Translations.ErrorFetchMsg),
                repositoryName),
            details);

        Context.API.ShowMsgError(
            Context.API.GetTranslation(Translations.Error),
            message);
    }
}
