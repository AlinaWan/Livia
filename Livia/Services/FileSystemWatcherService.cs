using System.IO;

namespace Livia.Services;

/// <summary>Provides reusable file and directory monitoring backed by <see cref="System.IO.FileSystemWatcher"/>.</summary>
/// <remarks>The service reports raw filesystem events. Consumers that reload files should debounce events and handle partially written files.</remarks>
public sealed class FileSystemWatcherService : IDisposable
{
    private readonly object _sync = new();
    private FileSystemWatcher? _watcher;
    private string? _watchedFile;
    private bool _disposed;
    private readonly List<Action<FileSystemChangeEventArgs>> _callbacks = [];

    /// <summary>Raised when a filesystem event occurs.</summary>
    public event EventHandler<FileSystemChangeEventArgs>? Changed;
    /// <summary>Raised when the underlying watcher reports an error.</summary>
    public event EventHandler<FileSystemChangeEventArgs>? Error;
    /// <summary>Gets whether monitoring is active.</summary>
    public bool IsWatching { get { lock (_sync) return _watcher is not null; } }

    /// <summary>Starts watching one file, including replacements performed by rename-and-move saves.</summary>
    public void StartFile(string filePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        string fullPath = Path.GetFullPath(filePath);
        string directory = Path.GetDirectoryName(fullPath) ?? throw new ArgumentException("The path must include a directory.", nameof(filePath));
        StartCore(directory, Path.GetFileName(fullPath), includeSubdirectories: false, fullPath);
    }

    /// <summary>Starts watching a directory using an optional filename filter.</summary>
    public void StartDirectory(string directoryPath, string filter = "*", bool includeSubdirectories = false)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(directoryPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(filter);
        string fullPath = Path.GetFullPath(directoryPath);
        if (!Directory.Exists(fullPath)) throw new DirectoryNotFoundException($"Directory not found: {fullPath}");
        StartCore(fullPath, filter, includeSubdirectories, null);
    }

    /// <summary>Stops monitoring. The service can be started again unless it has been disposed.</summary>
    public void Stop()
    {
        FileSystemWatcher? watcher;
        lock (_sync) { watcher = _watcher; _watcher = null; _watchedFile = null; }
        if (watcher is null) return;
        watcher.EnableRaisingEvents = false;
        watcher.Created -= OnCreated;
        watcher.Changed -= OnChanged;
        watcher.Deleted -= OnDeleted;
        watcher.Renamed -= OnRenamed;
        watcher.Error -= OnError;
        watcher.Dispose();
    }

    /// <summary>Registers a callback invoked for every reported filesystem event.</summary>
    public IDisposable RegisterCallback(Action<FileSystemChangeEventArgs> callback)
    {
        ArgumentNullException.ThrowIfNull(callback);
        lock (_sync) { ThrowIfDisposed(); _callbacks.Add(callback); }
        return new CallbackRegistration(this, callback);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        lock (_sync) { if (_disposed) return; _disposed = true; }
        Stop();
        GC.SuppressFinalize(this);
    }

    private void StartCore(string directory, string filter, bool includeSubdirectories, string? watchedFile)
    {
        lock (_sync) ThrowIfDisposed();
        Stop();
        FileSystemWatcher watcher = new(directory, filter)
        {
            IncludeSubdirectories = includeSubdirectories,
            NotifyFilter = NotifyFilters.FileName | NotifyFilters.DirectoryName | NotifyFilters.LastWrite | NotifyFilters.Size | NotifyFilters.CreationTime,
            EnableRaisingEvents = false
        };
        watcher.Created += OnCreated;
        watcher.Changed += OnChanged;
        watcher.Deleted += OnDeleted;
        watcher.Renamed += OnRenamed;
        watcher.Error += OnError;
        lock (_sync) { ThrowIfDisposed(); _watcher = watcher; _watchedFile = watchedFile; }
        watcher.EnableRaisingEvents = true;
    }

    private void OnCreated(object sender, FileSystemEventArgs e) => Publish(new(FileSystemChangeKind.Created, e.FullPath));
    private void OnChanged(object sender, FileSystemEventArgs e) => Publish(new(FileSystemChangeKind.Changed, e.FullPath));
    private void OnDeleted(object sender, FileSystemEventArgs e) => Publish(new(FileSystemChangeKind.Deleted, e.FullPath));
    private void OnRenamed(object sender, RenamedEventArgs e) => Publish(new(FileSystemChangeKind.Renamed, e.FullPath, e.OldFullPath));
    private void OnError(object sender, ErrorEventArgs e)
    {
        FileSystemChangeEventArgs args = new(FileSystemChangeKind.Error, exception: e.GetException());
        Error?.Invoke(this, args);
        PublishCallbacks(args);
    }

    private void Publish(FileSystemChangeEventArgs args)
    {
        lock (_sync)
        {
            if (_watchedFile is not null && args.FullPath is not null &&
                !string.Equals(Path.GetFullPath(args.FullPath), _watchedFile, OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal))
                return;
        }
        Changed?.Invoke(this, args);
        PublishCallbacks(args);
    }

    private void PublishCallbacks(FileSystemChangeEventArgs args)
    {
        Action<FileSystemChangeEventArgs>[] callbacks;
        lock (_sync) callbacks = _callbacks.ToArray();
        foreach (Action<FileSystemChangeEventArgs> callback in callbacks)
        {
            try { callback(args); }
            catch { /* A consumer callback must not break OS notification delivery. */ }
        }
    }

    private void RemoveCallback(Action<FileSystemChangeEventArgs> callback) { lock (_sync) _callbacks.Remove(callback); }
    private void ThrowIfDisposed() { if (_disposed) throw new ObjectDisposedException(nameof(FileSystemWatcherService)); }

    private sealed class CallbackRegistration(FileSystemWatcherService owner, Action<FileSystemChangeEventArgs> callback) : IDisposable
    {
        private FileSystemWatcherService? _owner = owner;
        public void Dispose() => Interlocked.Exchange(ref _owner, null)?.RemoveCallback(callback);
    }
}
