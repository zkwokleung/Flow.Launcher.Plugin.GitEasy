using Flow.Launcher.Plugin.GitEasy.Models.Processes;
using Flow.Launcher.Plugin.GitEasy.Services.Interfaces;
using System;
using System.Buffers;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Flow.Launcher.Plugin.GitEasy.Services;

public sealed class ProcessRunner : IProcessRunner
{
    public ProcessExecutionResult Run(
        ProcessStartInfo startInfo,
        CancellationToken cancellationToken = default)
    {
        return RunAsync(startInfo, cancellationToken)
            .ConfigureAwait(false)
            .GetAwaiter()
            .GetResult();
    }

    public async Task<ProcessExecutionResult> RunAsync(
        ProcessStartInfo startInfo,
        CancellationToken cancellationToken = default)
    {
        ValidateOwnedProcess(startInfo);
        cancellationToken.ThrowIfCancellationRequested();

        using var process = new Process { StartInfo = startInfo };
        StartProcess(process, startInfo.FileName);

        Task<string> standardOutputTask = startInfo.RedirectStandardOutput
            ? ReadToEndAsync(process.StandardOutput, cancellationToken)
            : Task.FromResult(string.Empty);
        Task<string> standardErrorTask = startInfo.RedirectStandardError
            ? ReadToEndAsync(process.StandardError, cancellationToken)
            : Task.FromResult(string.Empty);

        Task waitForExitTask = process.WaitForExitAsync(CancellationToken.None);
        Task completionTask = Task.WhenAll(
            waitForExitTask,
            standardOutputTask,
            standardErrorTask);

        try
        {
            await completionTask.WaitAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            await TerminateProcessTreeAsync(
                    process,
                    waitForExitTask,
                    standardOutputTask,
                    standardErrorTask)
                .ConfigureAwait(false);
            throw;
        }

        return new ProcessExecutionResult(
            process.ExitCode,
            standardOutputTask.Result,
            standardErrorTask.Result);
    }

    public void StartDetached(ProcessStartInfo startInfo)
    {
        ValidateStartInfo(startInfo);

        if (startInfo.RedirectStandardInput
            || startInfo.RedirectStandardOutput
            || startInfo.RedirectStandardError)
        {
            throw new ArgumentException(
                "Detached processes cannot use redirected standard streams.",
                nameof(startInfo));
        }

        using var process = new Process { StartInfo = startInfo };
        StartProcess(process, startInfo.FileName);
    }

    private static async Task TerminateProcessTreeAsync(
        Process process,
        Task waitForExitTask,
        Task<string> standardOutputTask,
        Task<string> standardErrorTask)
    {
        try
        {
            if (!HasExited(process))
            {
                process.Kill(entireProcessTree: true);
            }
        }
        catch (Exception exception) when (exception is InvalidOperationException
                                          or Win32Exception
                                          or NotSupportedException)
        {
            if (!HasExited(process))
            {
                throw new InvalidOperationException(
                    $"Process '{process.StartInfo.FileName}' could not be terminated after cancellation.",
                    exception);
            }
        }

        await waitForExitTask.ConfigureAwait(false);

        try
        {
            await Task.WhenAll(standardOutputTask, standardErrorTask).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            // The caller cancellation token also interrupts inherited pipe reads.
        }
        catch (IOException)
        {
            // A terminated process can close a redirected pipe while it is being drained.
        }
    }

    private static async Task<string> ReadToEndAsync(
        StreamReader reader,
        CancellationToken cancellationToken)
    {
        char[] buffer = ArrayPool<char>.Shared.Rent(4096);

        try
        {
            var output = new StringBuilder();

            while (true)
            {
                int charactersRead = await reader
                    .ReadAsync(buffer.AsMemory(), cancellationToken)
                    .ConfigureAwait(false);

                if (charactersRead == 0)
                {
                    return output.ToString();
                }

                output.Append(buffer, 0, charactersRead);
            }
        }
        finally
        {
            ArrayPool<char>.Shared.Return(buffer);
        }
    }

    private static bool HasExited(Process process)
    {
        try
        {
            return process.HasExited;
        }
        catch (InvalidOperationException)
        {
            return true;
        }
    }

    private static void StartProcess(Process process, string fileName)
    {
        bool started;
        try
        {
            started = process.Start();
        }
        catch (Win32Exception exception)
        {
            throw new InvalidOperationException(
                $"Process '{fileName}' could not be started.",
                exception);
        }

        if (!started)
        {
            throw new InvalidOperationException($"Process '{fileName}' could not be started.");
        }
    }

    private static void ValidateOwnedProcess(ProcessStartInfo startInfo)
    {
        ValidateStartInfo(startInfo);

        if (startInfo.RedirectStandardInput)
        {
            throw new ArgumentException(
                "Redirected standard input is not supported by the process runner.",
                nameof(startInfo));
        }

        if (startInfo.UseShellExecute
            && (startInfo.RedirectStandardOutput || startInfo.RedirectStandardError))
        {
            throw new ArgumentException(
                "Shell execution cannot be combined with redirected output.",
                nameof(startInfo));
        }
    }

    private static void ValidateStartInfo(ProcessStartInfo startInfo)
    {
        ArgumentNullException.ThrowIfNull(startInfo);

        if (string.IsNullOrWhiteSpace(startInfo.FileName))
        {
            throw new ArgumentException("A process executable is required.", nameof(startInfo));
        }
    }
}
