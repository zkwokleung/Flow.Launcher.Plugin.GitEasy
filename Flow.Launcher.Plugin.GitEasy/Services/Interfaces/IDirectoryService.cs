using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Flow.Launcher.Plugin.GitEasy.Services.Interfaces;

public interface IDirectoryService
{
    Task<IReadOnlyList<string>> GetExistingRepositoryRootsAsync(CancellationToken cancellationToken);
    void CreateDirectory(string path);
    Task<IReadOnlyList<string>> GetRepositoriesDirectoriesAsync(CancellationToken cancellationToken);
}
