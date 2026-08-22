using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Flow.Launcher.Plugin.GitEasy.Services.Interfaces;

public interface ICommandService
{
    Task<List<Result>> ResolveAsync(Query query, CancellationToken cancellationToken);
}
