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
    private const int MaximumCapturedCharactersPerStream = 64 * 1024;

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
            var output = new BoundedCharacterBuffer(MaximumCapturedCharactersPerStream);

            while (true)
            {
                int charactersRead = await reader
                    .ReadAsync(buffer.AsMemory(), cancellationToken)
                    .ConfigureAwait(false);

                if (charactersRead == 0)
                {
                    return output.ToString();
                }

                output.Append(buffer.AsSpan(0, charactersRead));
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

    private sealed class BoundedCharacterBuffer
    {
        private readonly char[] _buffer;
        private int _length;
        private int _writeIndex;

        public BoundedCharacterBuffer(int capacity)
        {
            _buffer = new char[capacity];
        }

        public void Append(ReadOnlySpan<char> value)
        {
            if (value.Length >= _buffer.Length)
            {
                value[^_buffer.Length..].CopyTo(_buffer);
                _length = _buffer.Length;
                _writeIndex = 0;
                return;
            }

            int firstSegmentLength = Math.Min(value.Length, _buffer.Length - _writeIndex);
            value[..firstSegmentLength].CopyTo(_buffer.AsSpan(_writeIndex));

            int secondSegmentLength = value.Length - firstSegmentLength;
            if (secondSegmentLength > 0)
            {
                value[firstSegmentLength..].CopyTo(_buffer);
            }

            _writeIndex = (_writeIndex + value.Length) % _buffer.Length;
            _length = Math.Min(_length + value.Length, _buffer.Length);
        }

        public override string ToString()
        {
            if (_length == 0)
            {
                return string.Empty;
            }

            int startIndex = (_writeIndex - _length + _buffer.Length) % _buffer.Length;
            if (startIndex + _length <= _buffer.Length)
            {
                return new string(_buffer, startIndex, _length);
            }

            int firstSegmentLength = _buffer.Length - startIndex;
            return new StringBuilder(_length)
                .Append(_buffer, startIndex, firstSegmentLength)
                .Append(_buffer, 0, _length - firstSegmentLength)
                .ToString();
        }
    }
}
