using Flow.Launcher.Plugin.GitEasy.Utilities;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Flow.Launcher.Plugin.GitEasy.Models.Commands.Interfaces;

public interface ICommand
{
    string Key { get; }
    string Title { get; }
    string Description { get; }
    string IconPath { get => Icons.Logo; }
    Task<List<Result>> ResolveAsync(
        string query,
        string actionKeyword,
        CancellationToken cancellationToken);
}
