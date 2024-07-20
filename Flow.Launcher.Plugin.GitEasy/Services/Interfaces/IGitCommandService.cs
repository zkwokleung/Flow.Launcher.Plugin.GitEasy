using Flow.Launcher.Plugin.GitEasy.Models.Commands.EventArgs;
using Flow.Launcher.Plugin.GitEasy.Models.Commands.Options;
using System;

namespace Flow.Launcher.Plugin.GitEasy.Services.Interfaces;

public interface IGitCommandService
{
    void CloneRepos(GitCloneCommandOptions options, Action OnCompleted = null);
    void FetchRepos(GitFetchCommandOptions options, Action<GitFetchCompletedEventArgs> OnCompleted = null);
}