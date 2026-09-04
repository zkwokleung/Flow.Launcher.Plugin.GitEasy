using Flow.Launcher.Plugin.GitEasy.Utilities;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace Flow.Launcher.Plugin.GitEasy.ViewModels;

internal sealed class SettingsPathValidationCoordinator
{
    private static readonly TimeSpan FlushTimeout = TimeSpan.FromSeconds(2);

    private readonly DispatcherTimer _timer;
    private readonly Func<SettingsPathProbeRequest> _captureRequest;
    private readonly Action<SettingsPathProbeResult, bool> _applyResult;
    private readonly Action<Exception> _handleError;
    private CancellationTokenSource _cancellationTokenSource;
    private PendingValidation _pendingValidation;
    private long _version;
    private bool _saveAfterValidation;
    private bool _workerRunning;

    public SettingsPathValidationCoordinator(
        TimeSpan delay,
        Func<SettingsPathProbeRequest> captureRequest,
        Action<SettingsPathProbeResult, bool> applyResult,
        Action<Exception> handleError)
    {
        _captureRequest = captureRequest ?? throw new ArgumentNullException(nameof(captureRequest));
        _applyResult = applyResult ?? throw new ArgumentNullException(nameof(applyResult));
        _handleError = handleError ?? throw new ArgumentNullException(nameof(handleError));
        _timer = new DispatcherTimer { Interval = delay };
        _timer.Tick += OnTimerTick;
    }

    public void Schedule(bool saveAfterValidation)
    {
        _version++;
        _saveAfterValidation = saveAfterValidation;
        _cancellationTokenSource?.Cancel();
        _timer.Stop();
        _timer.Start();
    }

    public async Task<bool> FlushAsync()
    {
        _version++;
        _saveAfterValidation = false;
        _timer.Stop();
        _cancellationTokenSource?.Cancel();

        var completion = new TaskCompletionSource<bool>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        var validation = new PendingValidation(
            _version,
            _captureRequest(),
            SaveAfterValidation: false,
            completion);
        EnqueueValidation(validation);

        try
        {
            return await completion.Task.WaitAsync(FlushTimeout);
        }
        catch (TimeoutException exception)
        {
            if (completion.Task.IsCompletedSuccessfully)
            {
                return completion.Task.Result;
            }

            if (validation.Version == _version)
            {
                _version++;
                _cancellationTokenSource?.Cancel();

                if (ReferenceEquals(_pendingValidation, validation))
                {
                    _pendingValidation = null;
                }

                _handleError(new TimeoutException(
                    "Path validation timed out. The latest path changes were not saved.",
                    exception));
            }

            completion.TrySetResult(false);
            return false;
        }
    }

    private void OnTimerTick(object sender, EventArgs e)
    {
        _timer.Stop();
        EnqueueValidation(
            new PendingValidation(
                _version,
                _captureRequest(),
                _saveAfterValidation,
                Completion: null));
    }

    private void EnqueueValidation(PendingValidation validation)
    {
        _pendingValidation?.Completion?.TrySetResult(false);
        _pendingValidation = validation;

        if (_workerRunning)
        {
            return;
        }

        _workerRunning = true;
        _ = ProcessPendingValidationsAsync();
    }

    private async Task ProcessPendingValidationsAsync()
    {
        while (_pendingValidation != null)
        {
            PendingValidation validation = _pendingValidation;
            _pendingValidation = null;
            await ProcessValidationAsync(validation);
        }

        _workerRunning = false;
    }

    private async Task ProcessValidationAsync(PendingValidation validation)
    {
        var cancellationTokenSource = new CancellationTokenSource();
        CancellationTokenSource previousCancellationTokenSource = _cancellationTokenSource;
        _cancellationTokenSource = cancellationTokenSource;
        previousCancellationTokenSource?.Cancel();
        bool resultApplied = false;

        try
        {
            SettingsPathProbeResult result = await SettingsPathProbe
                .ProbeAsync(validation.Request, cancellationTokenSource.Token);

            if (validation.Version == _version)
            {
                _applyResult(result, validation.SaveAfterValidation);
                resultApplied = true;
            }
        }
        catch (OperationCanceledException) when (cancellationTokenSource.IsCancellationRequested)
        {
            // A newer draft or an unloaded settings view superseded this validation.
        }
        catch (Exception exception)
        {
            if (validation.Version == _version)
            {
                _handleError(exception);
            }
        }
        finally
        {
            if (ReferenceEquals(_cancellationTokenSource, cancellationTokenSource))
            {
                _cancellationTokenSource = null;
            }

            cancellationTokenSource.Dispose();
            validation.Completion?.TrySetResult(resultApplied);
        }
    }

    private sealed record PendingValidation(
        long Version,
        SettingsPathProbeRequest Request,
        bool SaveAfterValidation,
        TaskCompletionSource<bool> Completion);
}
