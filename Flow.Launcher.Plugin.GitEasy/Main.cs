using Flow.Launcher.Plugin.GitEasy.Views;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Controls;
using Microsoft.Extensions.DependencyInjection;
using Flow.Launcher.Plugin.GitEasy.DI;
using Flow.Launcher.Plugin.GitEasy.Services.Interfaces;
using Flow.Launcher.Plugin.GitEasy.Utilities;

namespace Flow.Launcher.Plugin.GitEasy;

public partial class Main : ISettingProvider, IAsyncPlugin, IPluginI18n
{
    internal ServiceProvider ServiceProvider { get; private set; }

    private PluginInitContext m_context { get; set; }
    private ICommandService m_commandService { get; set; }
    private ISettingsService m_settingsService { get; set; }
    private IDirectoryService m_directoryService { get; set; }

    #region Flow.Launcher Interface Functions
    public async Task InitAsync(PluginInitContext context)
    {
        ServiceProvider = new ServiceCollection()
                                .InjectServices(context)
                                .InjectCommands()
                                .BuildServiceProvider();

        m_context = context;
        m_commandService = ServiceProvider.GetService<ICommandService>();
        m_settingsService = ServiceProvider.GetService<ISettingsService>();
        m_directoryService = ServiceProvider.GetService<IDirectoryService>();
    }

    public async Task<List<Result>> QueryAsync(Query query, CancellationToken token)
    {
        // Verify the repositories path before running any command
        if (!m_directoryService.VerifyRepositoriesPath())
        {
            return new()
            {
                new Result
                {
                    Title = m_context.API.GetTranslation(Translations.QueryInvalidReposPath),
                    SubTitle = m_context.API.GetTranslation(Translations.QueryOpenSettings),
                    IcoPath = Icons.Error,
                    Score = 1000,
                    Action = _ =>
                    {
                        m_context.API.OpenSettingDialog();
                        return true;
                    }
                },
                new Result
                {
                    Title = string.Format(m_context.API.GetTranslation(Translations.QueryCreateFolder), m_settingsService.GetSettingsOrDefault().ReposPath),
                    IcoPath = Icons.Explorer,
                    Action = _ =>
                    {
                        try
                        {
                            m_directoryService.CreateRepositoriesDirectory();
                            m_context.API.ShowMsg(string.Format(m_context.API.GetTranslation(Translations.MsgFolderCreated),m_settingsService.GetSettingsOrDefault().ReposPath));
                        }
                        catch (Exception ex)
                        {
                            m_context.API.ShowMsgError(
                                string.Format($"{m_context.API.GetTranslation(Translations.Error)}: {m_context.API.GetTranslation(Translations.ErrorCreateFolderFailed)}"),
                                ex.Message
                            );
                        }
                        return true;
                    }
                }
            };
        }

        return await m_commandService.Resolve(query);
    }

    public string GetTranslatedPluginTitle()
    {
        return m_context.API.GetTranslation(Translations.PluginTitle);
    }

    public string GetTranslatedPluginDescription()
    {
        return m_context.API.GetTranslation(Translations.PluginDesc);
    }

    public static void StartProcess(Func<ProcessStartInfo, Process> runProcess, ProcessStartInfo info)
    {

    }

    public Control CreateSettingPanel()
    {
        return new SettingsPanel(m_context, m_settingsService.GetSettingsOrDefault());
    }
    #endregion
}
