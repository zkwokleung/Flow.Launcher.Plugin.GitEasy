using Flow.Launcher.Plugin.GitEasy.Views;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Controls;
using Microsoft.Extensions.DependencyInjection;
using Flow.Launcher.Plugin.GitEasy.Configurations;
using Flow.Launcher.Plugin.GitEasy.Services.Interfaces;
using Flow.Launcher.Plugin.GitEasy.Utilities;

namespace Flow.Launcher.Plugin.GitEasy;

public partial class Main : ISettingProvider, IAsyncPlugin, IPluginI18n
{
    internal ServiceProvider ServiceProvider { get; private set; }

    private PluginInitContext _context;
    private ICommandService _commandService;
    private ISettingsService _settingsService;
    private IDirectoryService _directoryService;

    #region Flow.Launcher Interface Functions
    public async Task InitAsync(PluginInitContext context)
    {
        ServiceProvider = new ServiceCollection()
                                .InjectServices(context)
                                .InjectCommands()
                                .BuildServiceProvider();

        _context = context;
        _commandService = ServiceProvider.GetService<ICommandService>();
        _settingsService = ServiceProvider.GetService<ISettingsService>();
        _directoryService = ServiceProvider.GetService<IDirectoryService>();
    }

    public async Task<List<Result>> QueryAsync(Query query, CancellationToken token)
    {
        // Verify the repositories path before running any command
        if (!_directoryService.VerifyRepositoriesPath())
        {
            return new()
            {
                new Result
                {
                    Title = _context.API.GetTranslation(Translations.ErrorInvalidReposPath),
                    SubTitle = _context.API.GetTranslation(Translations.QueryOpenSettings),
                    IcoPath = Icons.Error,
                    Score = 1000,
                    Action = _ =>
                    {
                        _context.API.OpenSettingDialog();
                        return true;
                    }
                },
                new Result
                {
                    Title = string.Format(_context.API.GetTranslation(Translations.QueryCreateFolder), _settingsService.GetSettingsOrDefault().ReposPath),
                    IcoPath = Icons.Explorer,
                    Action = _ =>
                    {
                        try
                        {
                            _directoryService.CreateRepositoriesDirectory();
                            _context.API.ShowMsg(string.Format(_context.API.GetTranslation(Translations.MsgFolderCreated),_settingsService.GetSettingsOrDefault().ReposPath));
                        }
                        catch (Exception ex)
                        {
                            _context.API.ShowMsgError(
                                string.Format($"{_context.API.GetTranslation(Translations.Error)}: {_context.API.GetTranslation(Translations.ErrorCreateFolderFailed)}"),
                                ex.Message
                            );
                        }
                        return true;
                    }
                }
            };
        }

        return await _commandService.Resolve(query);
    }

    public string GetTranslatedPluginTitle()
    {
        return _context.API.GetTranslation(Translations.PluginTitle);
    }

    public string GetTranslatedPluginDescription()
    {
        return _context.API.GetTranslation(Translations.PluginDesc);
    }

    public static void StartProcess(Func<ProcessStartInfo, Process> runProcess, ProcessStartInfo info)
    {

    }

    public Control CreateSettingPanel()
    {
        return new SettingsMenu(_context, _settingsService.GetSettingsOrDefault());
    }
    #endregion
}
