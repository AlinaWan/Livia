using System;

namespace Livia.Services.ScreenCapture;

/// <summary>
/// Represents a captured screen frame backed by native memory.
/// </summary>
public sealed class Frame : IDisposable
{
    private readonly Action _unlock;
    private bool _disposed;

    internal Frame(
        IntPtr data,
        int width,
        int height,
        int rowPitch,
        Action unlock)
    {
        Data = data;
        Width = width;
        Height = height;
        RowPitch = rowPitch;
        _unlock = unlock;
    }

    /// <summary>
    /// Gets a pointer to the first pixel in the frame.
    /// </summary>
    public IntPtr Data
    {
        get;
    }

    /// <summary>
    /// Gets the width of the frame in pixels.
    /// </summary>
    public int Width
    {
        get;
    }

    /// <summary>
    /// Gets the height of the frame in pixels.
    /// </summary>
    public int Height
    {
        get;
    }

    /// <summary>
    /// Gets the number of bytes between the start of each row.
    /// </summary>
    public int RowPitch
    {
        get;
    }

    /// <summary>
    /// Gets the frame's BGRA8 pixel data.
    /// </summary>
    /// <exception cref="ObjectDisposedException">
    /// Thrown when the frame has already been disposed.
    /// </exception>
    public unsafe ReadOnlySpan<byte> Pixels
    {
        get
        {
            ThrowIfDisposed();

            return new ReadOnlySpan<byte>(
                (void*)Data,
                RowPitch * Height);
        }
    }

    /// <summary>
    /// Releases the native frame mapping.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _unlock();
        _disposed = true;
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(Frame));
        }
    }
}