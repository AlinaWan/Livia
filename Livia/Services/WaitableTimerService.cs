using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using Livia.Native.Handles;

namespace Livia.Services;

/// <summary>
/// Schedules asynchronous callbacks for specific UTC times.
/// </summary>
public sealed class WaitableTimerService : IDisposable
{
    private readonly object _sync = new();

    private RunState? _currentRun;
    private bool _disposed;

    /// <summary>
    /// Starts a timer that triggers at the specified UTC time of day.
    /// </summary>
    /// <param name="targetTimeUtc">
    /// The UTC time of day at which the callback should be triggered.
    /// </param>
    /// <param name="recurring">
    /// <see langword="true"/> to trigger every day at the specified time;
    /// <see langword="false"/> to trigger only once.
    /// </param>
    /// <param name="onTriggered">
    /// The asynchronous callback to invoke when the timer triggers.
    /// </param>
    /// <remarks>
    /// Starting the service again replaces the currently scheduled timer.
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="targetTimeUtc"/> is not a valid time of day.
    /// </exception>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="onTriggered"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="ObjectDisposedException">
    /// Thrown when the service has been disposed.
    /// </exception>
    public void Start(
        TimeSpan targetTimeUtc,
        bool recurring,
        Func<Task> onTriggered)
    {
        if (targetTimeUtc < TimeSpan.Zero ||
            targetTimeUtc >= TimeSpan.FromDays(1))
        {
            throw new ArgumentOutOfRangeException(nameof(targetTimeUtc));
        }

        ArgumentNullException.ThrowIfNull(onTriggered);

        Start(
            getNextTargetUtc: utcNow =>
            {
                DateTime target = utcNow.Date.Add(targetTimeUtc);

                if (target <= utcNow)
                    target = target.AddDays(1);

                return target;
            },
            recurring,
            onTriggered);
    }

    /// <summary>
    /// Starts a timer that triggers once at the specified UTC date and time.
    /// </summary>
    /// <param name="targetDateTimeUtc">
    /// The UTC date and time at which the callback should be triggered.
    /// </param>
    /// <param name="onTriggered">
    /// The asynchronous callback to invoke when the timer triggers.
    /// </param>
    /// <remarks>
    /// Starting the service again replaces the currently scheduled timer.
    /// </remarks>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="targetDateTimeUtc"/> does not have
    /// <see cref="DateTimeKind.Utc"/>.
    /// </exception>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="onTriggered"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="ObjectDisposedException">
    /// Thrown when the service has been disposed.
    /// </exception>
    public void Start(
        DateTime targetDateTimeUtc,
        Func<Task> onTriggered)
    {
        ArgumentNullException.ThrowIfNull(onTriggered);

        if (targetDateTimeUtc.Kind != DateTimeKind.Utc)
        {
            throw new ArgumentException(
                "The target date and time must have DateTimeKind.Utc.",
                nameof(targetDateTimeUtc));
        }

        Start(
            getNextTargetUtc: _ => targetDateTimeUtc,
            recurring: false,
            onTriggered);
    }

    /// <summary>
    /// Stops the currently scheduled timer.
    /// </summary>
    /// <remarks>
    /// This method requests cancellation and returns immediately. Use
    /// <see cref="StopAsync"/> when the caller must wait for the timer
    /// and its callback to finish shutting down.
    /// </remarks>
    /// <exception cref="ObjectDisposedException">
    /// Thrown when the service has been disposed.
    /// </exception>
    public void Stop()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        RunState? run;

        lock (_sync)
        {
            run = _currentRun;
            _currentRun = null;
        }

        run?.Cancel();
    }

    /// <summary>
    /// Stops the currently scheduled timer and waits for it to finish.
    /// </summary>
    /// <returns>
    /// A task that completes when the timer and its callback have finished.
    /// </returns>
    /// <exception cref="ObjectDisposedException">
    /// Thrown when the service has been disposed.
    /// </exception>
    public async Task StopAsync()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        RunState? run;

        lock (_sync)
        {
            run = _currentRun;
            _currentRun = null;
        }

        if (run is null)
            return;

        run.Cancel();

        await run.Task.ConfigureAwait(false);
    }

    /// <summary>
    /// Releases all resources used by the timer service.
    /// </summary>
    public void Dispose()
    {
        RunState? run;

        lock (_sync)
        {
            if (_disposed)
                return;

            _disposed = true;
            run = _currentRun;
            _currentRun = null;
        }

        run?.Cancel();
    }

    private void Start(
        Func<DateTime, DateTime> getNextTargetUtc,
        bool recurring,
        Func<Task> onTriggered)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        RunState run = new();

        lock (_sync)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);

            _currentRun?.Cancel();
            _currentRun = run;

            run.Task = RunAsync(
                run,
                getNextTargetUtc,
                recurring,
                onTriggered);
        }
    }

    private async Task RunAsync(
        RunState run,
        Func<DateTime, DateTime> getNextTargetUtc,
        bool recurring,
        Func<Task> onTriggered)
    {
        using WaitableTimerHandle timer = WaitableTimerHandle.Create();

        try
        {
            while (!run.CancellationToken.IsCancellationRequested)
            {
                DateTime utcNow = DateTime.UtcNow;
                DateTime targetUtc = getNextTargetUtc(utcNow);

                if (targetUtc.Kind != DateTimeKind.Utc)
                {
                    throw new ArgumentException(
                        "The timer target must have DateTimeKind.Utc.",
                        nameof(getNextTargetUtc));
                }

                if (targetUtc <= utcNow)
                {
                    throw new ArgumentException(
                        "The timer target must be in the future.",
                        nameof(getNextTargetUtc));
                }

                if (!timer.Set(targetUtc))
                    throw new Win32Exception();

                using CancellationTokenRegistration cancellationRegistration =
                    run.CancellationToken.Register(() => timer.Cancel());

                uint result = await Task.Run(
                    timer.Wait,
                    CancellationToken.None).ConfigureAwait(false);

                if (run.CancellationToken.IsCancellationRequested)
                    return;

                if (result != 0)
                    throw new Win32Exception();

                await onTriggered().ConfigureAwait(false);

                if (!recurring)
                    return;
            }
        }
        finally
        {
            lock (_sync)
            {
                if (ReferenceEquals(_currentRun, run))
                    _currentRun = null;
            }

            run.Dispose();
        }
    }

    private sealed class RunState : IDisposable
    {
        private readonly CancellationTokenSource _cts = new();

        internal Task Task { get; set; } = Task.CompletedTask;

        internal CancellationToken CancellationToken =>
            _cts.Token;

        internal void Cancel()
        {
            _cts.Cancel();
        }

        public void Dispose()
        {
            _cts.Dispose();
        }
    }
}