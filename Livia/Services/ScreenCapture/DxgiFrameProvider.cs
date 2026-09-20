using System;
using Livia.Native.ScreenCapture;

namespace Livia.Services.ScreenCapture;

/// <summary>
/// Provides screen frames using the DXGI Desktop Duplication API.
/// </summary>
public sealed class DxgiFrameProvider : IFrameProvider
{
    private IntPtr _context;
    private bool _frameLocked;

    /// <summary>
    /// Initializes a new DXGI frame provider.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the DXGI capture context cannot be initialized.
    /// </exception>
    public DxgiFrameProvider()
    {
        _context = DxgiFrameCapture.InitContext();

        if (_context == IntPtr.Zero)
        {
            throw new InvalidOperationException(
                "Failed to initialize the DXGI capture context.");
        }
    }

    /// <inheritdoc/>
    public Frame? Grab(CaptureRegion region)
    {
        ThrowIfDisposed();

        if (_frameLocked)
        {
            throw new InvalidOperationException(
                "The previous frame must be disposed before capturing another frame.");
        }

        if (region.Width <= 0 || region.Height <= 0)
        {
            throw new ArgumentException(
                "The capture region must have a positive width and height.",
                nameof(region));
        }

        IntPtr data = DxgiFrameCapture.GrabFramePointer(
            _context,
            region.Left,
            region.Top,
            region.Right,
            region.Bottom,
            out int rowPitch);

        if (data == IntPtr.Zero)
        {
            return null;
        }

        _frameLocked = true;

        return new Frame(
            data,
            region.Width,
            region.Height,
            rowPitch,
            UnlockFrame);
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (_context == IntPtr.Zero)
        {
            return;
        }

        if (_frameLocked)
        {
            UnlockFrame();
        }

        DxgiFrameCapture.CloseContext(_context);
        _context = IntPtr.Zero;
    }

    private void UnlockFrame()
    {
        if (!_frameLocked || _context == IntPtr.Zero)
        {
            return;
        }

        DxgiFrameCapture.UnlockFramePointer(_context);
        _frameLocked = false;
    }

    private void ThrowIfDisposed()
    {
        if (_context == IntPtr.Zero)
        {
            throw new ObjectDisposedException(nameof(DxgiFrameProvider));
        }
    }
}