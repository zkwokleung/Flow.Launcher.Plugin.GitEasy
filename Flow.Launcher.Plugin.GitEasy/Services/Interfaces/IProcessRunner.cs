using Flow.Launcher.Plugin.GitEasy.Models.Processes;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Flow.Launcher.Plugin.GitEasy.Services.Interfaces;

public interface IProcessRunner
{
    ProcessExecutionResult Run(
        ProcessStartInfo startInfo,
        CancellationToken cancellationToken = default);

    Task<ProcessExecutionResult> RunAsync(
        ProcessStartInfo startInfo,
        CancellationToken cancellationToken = default);

    void StartDetached(ProcessStartInfo startInfo);
}
