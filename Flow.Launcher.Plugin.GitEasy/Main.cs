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
    }

    public async Task<List<Result>> QueryAsync(Query query, CancellationToken token)
    {
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
