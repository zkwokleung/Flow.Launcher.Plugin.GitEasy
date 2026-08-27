using Flow.Launcher.Plugin.GitEasy.Services.Interfaces;
using Flow.Launcher.Plugin.GitEasy.Utilities;
using System;
using System.Threading.Tasks;

namespace Flow.Launcher.Plugin.GitEasy.Models.Commands;

public class OpenCommand : RepositoryCommandBase
{
    public override string Key => "Open";
    public override string Title => Context.API.GetTranslation(Translations.QueryResultOpen);
    public override string Description => Context.API.GetTranslation(Translations.QueryResultOpenDesc);
    public override string IconPath => Icons.Logo;

    private readonly ISettingsService _settingsService;
    private readonly ISystemCommandService _systemCommandService;

    public OpenCommand(
        PluginInitContext context,
        ISettingsService settingsService,
        IDirectoryService directoryService,
        ISystemCommandService systemCommandService)
        : base(context, directoryService)
    {
        _settingsService = settingsService;
        _systemCommandService = systemCommandService;
    }

    protected override string ResultMessageTranslationKey => Translations.QueryResultOpenMsg;

    protected override Func<string, string, Task> CreateRepositoryAction()
    {
        OpenOption openOption = _settingsService.GetSettings().OpenReposIn;
        return (repositoryPath, _) => OpenRepositoryAsync(repositoryPath, openOption);
    }

    private async Task OpenRepositoryAsync(string repositoryPath, OpenOption openOption)
    {
        try
        {
            switch (openOption)
            {
                case OpenOption.VSCode:
                    await _systemCommandService.OpenVsCodeAsync(repositoryPath);
                    break;

                case OpenOption.Cursor:
                    await _systemCommandService.OpenCursorAsync(repositoryPath);
                    break;

                case OpenOption.FileExplorer:
                case OpenOption.None:
                default:
                    await _systemCommandService.OpenExplorerAsync(repositoryPath);
                    break;
            }
        }
        catch (Exception exception)
        {
            ShowOpenRepositoryError(repositoryPath, exception);
        }
    }

    private void ShowOpenRepositoryError(string repositoryPath, Exception exception)
    {
        string message = CommandErrorFormatter.FormatWithDetails(
            string.Format(
                Context.API.GetTranslation(Translations.ErrorOpenRepository),
                repositoryPath),
            exception.Message);

        Context.API.ShowMsgError(
            Context.API.GetTranslation(Translations.Error),
            message);
    }
}
