using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Threading.Channels;
using Livia.Native;

namespace Livia.Services;

/// <summary>
/// Monitors Roblox processes and closes their singleton mutex and event
/// handles to allow multiple Roblox instances to run simultaneously.
/// </summary>
/// <remarks>
/// This service requires elevated permissions to successfully access
/// and close handles owned by Roblox processes.
/// </remarks>
public sealed class RobloxSingletonMutexClosingService : IDisposable
{
    private const string RobloxProcessName = "RobloxPlayerBeta";

    private const string SingletonMutexName =
        @"\Sessions\1\BaseNamedObjects\ROBLOX_singletonMutex";

    private const string SingletonEventName =
        @"\Sessions\1\BaseNamedObjects\ROBLOX_singletonEvent";

    private static readonly TimeSpan ProcessCheckInterval =
        TimeSpan.FromMilliseconds(100);

    private static readonly TimeSpan InitialCloseDelay =
        TimeSpan.FromSeconds(3);

    private static readonly TimeSpan CloseRetryInterval =
        TimeSpan.FromSeconds(1);

    private const int CloseAttempts = 3;

    private readonly object _sync = new();

    private CancellationTokenSource? _cancellation;
    private Channel<int>? _processQueue;
    private Task? _monitorTask;
    private Task? _workerTask;

    private bool _running;
    private bool _disposed;

    /// <summary>
    /// Gets a value indicating whether the service is currently monitoring
    /// Roblox processes.
    /// </summary>
    public bool IsRunning
    {
        get
        {
            lock (_sync)
            {
                return _running;
            }
        }
    }

    /// <summary>
    /// Occurs after one or more Roblox singleton handles have been closed.
    /// </summary>
    public event EventHandler<
        RobloxSingletonHandlesClosedEventArgs>? SingletonHandlesClosed;

    /// <summary>
    /// Starts monitoring for Roblox processes.
    /// </summary>
    /// <exception cref="ObjectDisposedException">
    /// The service has already been disposed.
    /// </exception>
    public void Start()
    {
        CancellationTokenSource cancellation;
        Channel<int> processQueue;

        lock (_sync)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);

            if (_running)
                return;

            cancellation = new CancellationTokenSource();

            processQueue = Channel.CreateUnbounded<int>(
                new UnboundedChannelOptions
                {
                    SingleReader = true,
                    SingleWriter = false,
                    AllowSynchronousContinuations = false
                });

            _cancellation = cancellation;
            _processQueue = processQueue;
            _running = true;

            _workerTask = ProcessQueueAsync(
                processQueue.Reader,
                cancellation.Token);

            _monitorTask = MonitorProcessesAsync(
                processQueue.Writer,
                cancellation.Token);
        }
    }

    /// <summary>
    /// Stops monitoring for Roblox processes and waits for the background
    /// monitoring and handle-closing operations to finish.
    /// </summary>
    public void Stop()
    {
        CancellationTokenSource? cancellation;
        Task? monitorTask;
        Task? workerTask;

        lock (_sync)
        {
            if (!_running)
                return;

            _running = false;

            cancellation = _cancellation;
            monitorTask = _monitorTask;
            workerTask = _workerTask;

            _cancellation = null;
            _processQueue = null;
            _monitorTask = null;
            _workerTask = null;
        }

        cancellation?.Cancel();

        WaitForTask(monitorTask);
        WaitForTask(workerTask);

        cancellation?.Dispose();
    }

    /// <summary>
    /// Closes the Roblox singleton mutex and event handles belonging to the
    /// specified process.
    /// </summary>
    /// <param name="processId">
    /// The process ID of the Roblox process.
    /// </param>
    /// <returns>
    /// <see langword="true"/> if at least one singleton handle was closed;
    /// otherwise, <see langword="false"/>.
    /// </returns>
    /// <exception cref="ObjectDisposedException">
    /// The service has already been disposed.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="processId"/> is zero or negative.
    /// </exception>
    public bool CloseSingletonHandles(int processId)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(processId);

        CloseResult result = CloseSingletonHandlesCore(processId);

        if (result.HasClosedHandles)
            OnSingletonHandlesClosed(processId, result);

        return result.HasClosedHandles;
    }

    /// <summary>
    /// Releases all resources used by the service.
    /// </summary>
    public void Dispose()
    {
        lock (_sync)
        {
            if (_disposed)
                return;

            _disposed = true;
        }

        Stop();
    }

    private async Task MonitorProcessesAsync(
        ChannelWriter<int> processQueue,
        CancellationToken cancellationToken)
    {
        var knownProcessIds = new HashSet<int>();

        try
        {
            foreach (Process process in
                     Process.GetProcessesByName(RobloxProcessName))
            {
                using (process)
                {
                    knownProcessIds.Add(process.Id);

                    await processQueue.WriteAsync(
                        process.Id,
                        cancellationToken);
                }
            }

            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();

                Process[] processes =
                    Process.GetProcessesByName(RobloxProcessName);

                var currentProcessIds = new HashSet<int>();

                foreach (Process process in processes)
                {
                    using (process)
                    {
                        int processId = process.Id;

                        currentProcessIds.Add(processId);

                        if (knownProcessIds.Contains(processId))
                            continue;

                        knownProcessIds.Add(processId);

                        await processQueue.WriteAsync(
                            processId,
                            cancellationToken);
                    }
                }

                knownProcessIds.IntersectWith(
                    currentProcessIds);

                await Task.Delay(
                    ProcessCheckInterval,
                    cancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
        }
    }

    private async Task ProcessQueueAsync(
        ChannelReader<int> processQueue,
        CancellationToken cancellationToken)
    {
        try
        {
            await foreach (int processId in
                           processQueue.ReadAllAsync(
                               cancellationToken))
            {
                await ProcessRobloxAsync(
                    processId,
                    cancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
        }
    }

    private async Task ProcessRobloxAsync(
        int processId,
        CancellationToken cancellationToken)
    {
        await Task.Delay(
            InitialCloseDelay,
            cancellationToken);

        for (int attempt = 0;
             attempt < CloseAttempts;
             attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            CloseResult result =
                CloseSingletonHandlesCore(processId);

            if (result.HasClosedHandles)
            {
                OnSingletonHandlesClosed(
                    processId,
                    result);
            }

            if (attempt + 1 < CloseAttempts)
            {
                await Task.Delay(
                    CloseRetryInterval,
                    cancellationToken);
            }
        }
    }

    private static CloseResult CloseSingletonHandlesCore(
        int processId)
    {
        IntPtr sourceProcess = Kernel32.OpenProcess(
            Kernel32.PROCESS_DUP_HANDLE,
            false,
            processId);

        if (sourceProcess == IntPtr.Zero)
            return default;

        try
        {
            List<Ntdll.SystemHandleTableEntryInfoEx> handles =
                QuerySystemHandles();

            int mutexesClosed = 0;
            int eventsClosed = 0;

            foreach (
                Ntdll.SystemHandleTableEntryInfoEx entry in handles)
            {
                if (entry.UniqueProcessId.ToInt64() != processId)
                    continue;

                if (!TryDuplicateHandle(
                        sourceProcess,
                        entry.HandleValue,
                        out IntPtr duplicateHandle))
                {
                    continue;
                }

                try
                {
                    if (!TryGetObjectName(
                            duplicateHandle,
                            out string? objectName))
                    {
                        continue;
                    }

                    bool isMutex =
                        IsSingletonMutex(objectName);

                    bool isEvent =
                        IsSingletonEvent(objectName);

                    if (!isMutex && !isEvent)
                        continue;

                    if (!TryCloseRemoteHandle(
                            sourceProcess,
                            entry.HandleValue))
                    {
                        continue;
                    }

                    if (isMutex)
                        mutexesClosed++;
                    else
                        eventsClosed++;
                }
                finally
                {
                    Kernel32.CloseHandle(duplicateHandle);
                }
            }

            return new CloseResult(
                mutexesClosed,
                eventsClosed);
        }
        finally
        {
            Kernel32.CloseHandle(sourceProcess);
        }
    }

    private static bool IsSingletonMutex(
        string objectName)
    {
        return string.Equals(
            objectName,
            SingletonMutexName,
            StringComparison.OrdinalIgnoreCase)
            || string.Equals(
                objectName,
                "ROBLOX_singletonMutex",
                StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsSingletonEvent(
        string objectName)
    {
        return string.Equals(
            objectName,
            SingletonEventName,
            StringComparison.OrdinalIgnoreCase)
            || string.Equals(
                objectName,
                "ROBLOX_singletonEvent",
                StringComparison.OrdinalIgnoreCase);
    }

    private static bool TryDuplicateHandle(
        IntPtr sourceProcess,
        IntPtr sourceHandle,
        out IntPtr duplicateHandle)
    {
        duplicateHandle = IntPtr.Zero;

        return Kernel32.DuplicateHandle(
            sourceProcess,
            sourceHandle,
            Process.GetCurrentProcess().Handle,
            out duplicateHandle,
            0,
            false,
            Kernel32.DUPLICATE_SAME_ACCESS);
    }

    private static bool TryCloseRemoteHandle(
        IntPtr sourceProcess,
        IntPtr sourceHandle)
    {
        if (!Kernel32.DuplicateHandle(
                sourceProcess,
                sourceHandle,
                Process.GetCurrentProcess().Handle,
                out IntPtr duplicateHandle,
                0,
                false,
                Kernel32.DUPLICATE_CLOSE_SOURCE |
                Kernel32.DUPLICATE_SAME_ACCESS))
        {
            return false;
        }

        Kernel32.CloseHandle(duplicateHandle);

        return true;
    }

    private static List<Ntdll.SystemHandleTableEntryInfoEx>
        QuerySystemHandles()
    {
        int bufferSize = 1024 * 1024;

        while (true)
        {
            IntPtr buffer =
                Marshal.AllocHGlobal(bufferSize);

            try
            {
                uint status =
                    Ntdll.NtQuerySystemInformation(
                        Ntdll.SystemExtendedHandleInformation,
                        buffer,
                        bufferSize,
                        out int returnLength);

                if (status == Ntdll.StatusSuccess)
                    return ReadHandleTable(buffer);

                if (status != Ntdll.StatusInfoLengthMismatch)
                {
                    throw new InvalidOperationException(
                        $"NtQuerySystemInformation failed " +
                        $"with NTSTATUS 0x{status:X8}.");
                }

                bufferSize = Math.Max(
                    bufferSize * 2,
                    returnLength);
            }
            finally
            {
                Marshal.FreeHGlobal(buffer);
            }
        }
    }

    private static List<Ntdll.SystemHandleTableEntryInfoEx>
        ReadHandleTable(IntPtr buffer)
    {
        long handleCount =
            Marshal.ReadIntPtr(buffer).ToInt64();

        if (handleCount < 0 ||
            handleCount > int.MaxValue)
        {
            throw new InvalidOperationException(
                "Invalid system handle count.");
        }

        int headerSize = IntPtr.Size * 2;

        int entrySize =
            Marshal.SizeOf<
                Ntdll.SystemHandleTableEntryInfoEx>();

        var handles =
            new List<
                Ntdll.SystemHandleTableEntryInfoEx>(
                (int)handleCount);

        IntPtr entryAddress =
            IntPtr.Add(
                buffer,
                headerSize);

        for (long i = 0; i < handleCount; i++)
        {
            Ntdll.SystemHandleTableEntryInfoEx entry =
                Marshal.PtrToStructure<
                    Ntdll.SystemHandleTableEntryInfoEx>(
                    entryAddress);

            handles.Add(entry);

            entryAddress = IntPtr.Add(
                entryAddress,
                entrySize);
        }

        return handles;
    }

    private static bool TryGetObjectName(
        IntPtr handle,
        [NotNullWhen(true)] out string? objectName)
    {
        objectName = null;

        int bufferSize = 1024;

        while (true)
        {
            IntPtr buffer =
                Marshal.AllocHGlobal(bufferSize);

            try
            {
                uint status =
                    Ntdll.NtQueryObject(
                        handle,
                        Ntdll.ObjectNameInformation,
                        buffer,
                        bufferSize,
                        out int returnLength);

                if (status == Ntdll.StatusSuccess)
                {
                    Ntdll.UnicodeString unicodeString =
                        Marshal.PtrToStructure<
                            Ntdll.UnicodeString>(
                            buffer);

                    if (unicodeString.Buffer ==
                            IntPtr.Zero ||
                        unicodeString.Length == 0)
                    {
                        return false;
                    }

                    objectName =
                        Marshal.PtrToStringUni(
                            unicodeString.Buffer,
                            unicodeString.Length / 2);

                    return objectName is not null;
                }

                if (status != Ntdll.StatusInfoLengthMismatch)
                    return false;

                bufferSize = Math.Max(
                    bufferSize * 2,
                    returnLength);
            }
            finally
            {
                Marshal.FreeHGlobal(buffer);
            }
        }
    }

    private void OnSingletonHandlesClosed(
        int processId,
        CloseResult result)
    {
        SingletonHandlesClosed?.Invoke(
            this,
            new RobloxSingletonHandlesClosedEventArgs(
                processId,
                result.MutexesClosed,
                result.EventsClosed));
    }

    private static void WaitForTask(Task? task)
    {
        if (task is null)
            return;

        try
        {
            task.GetAwaiter().GetResult();
        }
        catch (OperationCanceledException)
        {
        }
    }

    private readonly record struct CloseResult(
        int MutexesClosed,
        int EventsClosed)
    {
        public bool HasClosedHandles =>
            MutexesClosed > 0 || EventsClosed > 0;
    }
}
