using Flow.Launcher.Plugin.GitEasy.Models.Commands.EventArgs;
using Flow.Launcher.Plugin.GitEasy.Models.Commands.Options;
using Flow.Launcher.Plugin.GitEasy.Models.Commands.Results;
using System;

namespace Flow.Launcher.Plugin.GitEasy.Services.Interfaces;

public interface IGitCommandService
{
    GitCommandResult CloneRepos(GitCloneCommandOptions options);
    void FetchRepos(GitFetchCommandOptions options, Action<GitFetchCompletedEventArgs> OnCompleted = null);
}
