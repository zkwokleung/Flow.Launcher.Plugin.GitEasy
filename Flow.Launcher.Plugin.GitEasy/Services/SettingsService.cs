using Flow.Launcher.Plugin.GitEasy.Models;
using Flow.Launcher.Plugin.GitEasy.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI;

namespace Flow.Launcher.Plugin.GitEasy.Services;

public class SettingsService : ISettingsService
{
    private PluginInitContext m_context;
    private Settings m_settings;

    public SettingsService(PluginInitContext context)
    {
        m_context = context;
        m_settings = m_context.API.LoadSettingJsonStorage<Settings>();
    }

    public Settings GetDefault()
    {
        return new();
    }

    public Settings GetSettings()
    {
        return m_settings;
    }

    public Settings GetSettingsOrDefault()
    {
        return m_settings ??= new();
    }
}
