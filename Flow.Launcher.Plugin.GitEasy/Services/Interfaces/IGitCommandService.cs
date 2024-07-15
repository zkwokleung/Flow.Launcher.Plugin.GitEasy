using Flow.Launcher.Plugin.GitEasy.Models.Commands.Options;
using System;

namespace Flow.Launcher.Plugin.GitEasy.Services.Interfaces;

public interface IGitCommandService
{
    public void CloneRepos(GitCloneCommandOptions options, Action OnCompleted = null);
}