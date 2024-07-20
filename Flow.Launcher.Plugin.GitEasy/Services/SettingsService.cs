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
    private PluginInitContext _context;
    private Settings _settings;

    public SettingsService(PluginInitContext context)
    {
        _context = context;
        _settings = _context.API.LoadSettingJsonStorage<Settings>();
    }

    public Settings GetDefault()
    {
        return new();
    }

    public Settings GetSettings()
    {
        return _settings;
    }

    public Settings GetSettingsOrDefault()
    {
        return _settings ??= new();
    }
}
