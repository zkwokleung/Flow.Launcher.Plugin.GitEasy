using Flow.Launcher.Plugin.GitEasy.Models;

namespace Flow.Launcher.Plugin.GitEasy.Services.Interfaces;

public interface ISettingsService
{
    Settings GetSettingsOrDefault();
    Settings GetSettings();
    Settings GetDefault();
}
