using System;

namespace Livia.Services;

/// <summary>
/// Provides information about singleton handles closed for a Roblox process.
/// </summary>
public sealed class RobloxSingletonHandlesClosedEventArgs : EventArgs
{
    /// <summary>
    /// Initializes a new instance of the
    /// <see cref="RobloxSingletonHandlesClosedEventArgs"/> class.
    /// </summary>
    /// <param name="processId">
    /// The process ID of the Roblox process.
    /// </param>
    /// <param name="mutexesClosed">
    /// The number of singleton mutex handles that were closed.
    /// </param>
    /// <param name="eventsClosed">
    /// The number of singleton event handles that were closed.
    /// </param>
    public RobloxSingletonHandlesClosedEventArgs(
        int processId,
        int mutexesClosed,
        int eventsClosed)
    {
        ProcessId = processId;
        MutexesClosed = mutexesClosed;
        EventsClosed = eventsClosed;
    }

    /// <summary>
    /// Gets the process ID of the Roblox process.
    /// </summary>
    public int ProcessId
    {
        get;
    }

    /// <summary>
    /// Gets the number of singleton mutex handles that were closed.
    /// </summary>
    public int MutexesClosed
    {
        get;
    }

    /// <summary>
    /// Gets the number of singleton event handles that were closed.
    /// </summary>
    public int EventsClosed
    {
        get;
    }
}
