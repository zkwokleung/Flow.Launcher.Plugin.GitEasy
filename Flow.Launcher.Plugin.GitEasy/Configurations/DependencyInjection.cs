using Flow.Launcher.Plugin.GitEasy.Models.Commands;
using Flow.Launcher.Plugin.GitEasy.Models.Commands.Interfaces;
using Flow.Launcher.Plugin.GitEasy.Services;
using Flow.Launcher.Plugin.GitEasy.Services.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace Flow.Launcher.Plugin.GitEasy.DI;

public static class DependencyInjection
{
    public static IServiceCollection InjectServices(this IServiceCollection services, PluginInitContext context)
    {
        services.AddSingleton(context);
        services.AddSingleton<ISettingsService, SettingsService>();
        services.AddSingleton<ICommandService, CommandService>();
        services.AddSingleton<ISystemCommandService, SystemCommandService>();
        services.AddSingleton<IGitCommandService, GitCommandService>();
        services.AddSingleton<IGitHubService, GitHubService>();

        return services;
    }

    public static IServiceCollection InjectCommands(this IServiceCollection services)
    {
        services.AddSingleton<ICommand, CloneCommand>();

        return services;
    }
}
