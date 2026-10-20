namespace Livia.Services;

/// <summary>Identifies the kind of filesystem change reported by <see cref="FileSystemWatcherService"/>.</summary>
public enum FileSystemChangeKind
{
    /// <summary>A file or directory was created.</summary>
    Created,
    /// <summary>A file or directory changed.</summary>
    Changed,
    /// <summary>A file or directory was deleted.</summary>
    Deleted,
    /// <summary>A file or directory was renamed.</summary>
    Renamed,
    /// <summary>The underlying watcher reported an error.</summary>
    Error
}

/// <summary>Provides details about a filesystem notification.</summary>
public sealed class FileSystemChangeEventArgs : EventArgs
{
    /// <summary>Gets the kind of change.</summary>
    public FileSystemChangeKind Kind { get; }
    /// <summary>Gets the affected path, if available.</summary>
    public string? FullPath { get; }
    /// <summary>Gets the old path for a rename event.</summary>
    public string? OldFullPath { get; }
    /// <summary>Gets the underlying exception for an error event.</summary>
    public Exception? Exception { get; }

    internal FileSystemChangeEventArgs(FileSystemChangeKind kind, string? fullPath = null, string? oldFullPath = null, Exception? exception = null)
    {
        Kind = kind;
        FullPath = fullPath;
        OldFullPath = oldFullPath;
        Exception = exception;
    }
}
