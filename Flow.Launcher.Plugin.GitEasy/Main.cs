using Flow.Launcher.Plugin.GitEasy.Configurations;
using Flow.Launcher.Plugin.GitEasy.Services.Interfaces;
using Flow.Launcher.Plugin.GitEasy.Utilities;
using Flow.Launcher.Plugin.GitEasy.Views;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace Flow.Launcher.Plugin.GitEasy;

public partial class Main : ISettingProvider, IAsyncPlugin, IPluginI18n
{
    internal ServiceProvider ServiceProvider { get; private set; }

    private PluginInitContext _context;
    private ICommandService _commandService;
    private ISettingsService _settingsService;
    private IDirectoryService _directoryService;

    public Task InitAsync(PluginInitContext context)
    {
        ServiceProvider = new ServiceCollection()
            .InjectServices(context)
            .InjectCommands()
            .BuildServiceProvider();

        _context = context;
        _commandService = ServiceProvider.GetService<ICommandService>();
        _settingsService = ServiceProvider.GetService<ISettingsService>();
        _directoryService = ServiceProvider.GetService<IDirectoryService>();

        return Task.CompletedTask;
    }

    public async Task<List<Result>> QueryAsync(Query query, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        IReadOnlyList<string> existingRoots = await _directoryService
            .GetExistingRepositoryRootsAsync(token);

        if (existingRoots.Count > 0)
        {
            return await _commandService.ResolveAsync(query, token);
        }

        token.ThrowIfCancellationRequested();

        var results = new List<Result>
        {
            new()
            {
                Title = _context.API.GetTranslation(Translations.ErrorInvalidReposPath),
                SubTitle = _context.API.GetTranslation(Translations.QueryOpenSettings),
                IcoPath = Icons.Error,
                Score = 1000,
                Action = _ =>
                {
                    _context.API.OpenSettingDialog();
                    return true;
                },
            },
        };

        string repositoryPath = _settingsService.GetSettings()
            .ReposPaths
            .FirstOrDefault();

        if (repositoryPath == null)
        {
            token.ThrowIfCancellationRequested();
            return results;
        }

        results.Add(new Result
        {
            Title = string.Format(
                _context.API.GetTranslation(Translations.QueryCreateFolder),
                repositoryPath),
            IcoPath = Icons.Explorer,
            Action = _ =>
            {
                try
                {
                    _directoryService.CreateDirectory(repositoryPath);
                    _context.API.ShowMsg(string.Format(
                        _context.API.GetTranslation(Translations.MsgFolderCreated),
                        repositoryPath));
                }
                catch (Exception exception)
                {
                    string message = string.Format(
                        _context.API.GetTranslation(Translations.ErrorCreateFolderFailed),
                        repositoryPath);

                    _context.API.ShowMsgError(
                        _context.API.GetTranslation(Translations.Error),
                        $"{message}{Environment.NewLine}{exception.Message}");
                }

                return true;
            },
        });

        token.ThrowIfCancellationRequested();
        return results;
    }

    public string GetTranslatedPluginTitle()
    {
        return _context.API.GetTranslation(Translations.PluginTitle);
    }

    public string GetTranslatedPluginDescription()
    {
        return _context.API.GetTranslation(Translations.PluginDesc);
    }

    public Control CreateSettingPanel()
    {
        return new SettingsMenu(
            _context,
            _settingsService.GetSettings(),
            _settingsService.SaveSettings);
    }
}
