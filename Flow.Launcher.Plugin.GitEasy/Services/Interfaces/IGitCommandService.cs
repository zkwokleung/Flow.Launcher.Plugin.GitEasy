using Flow.Launcher.Plugin.GitEasy.Models.Commands.Options;
using Flow.Launcher.Plugin.GitEasy.Models.Commands.Results;
using System.Threading;
using System.Threading.Tasks;

namespace Flow.Launcher.Plugin.GitEasy.Services.Interfaces;

public interface IGitCommandService
{
    Task<GitCommandResult> CloneRepositoryAsync(
        GitCloneCommandOptions options,
        CancellationToken cancellationToken);
    Task<GitCommandResult> FetchRepositoryAsync(
        string repositoryPath,
        CancellationToken cancellationToken);
}
