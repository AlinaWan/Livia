using System;
using System.Windows.Controls;

namespace Livia.Services.ScreenCapture;

/// <summary>
/// Provides access to captured screen frames.
/// </summary>
public interface IFrameProvider : IDisposable
{
    /// <summary>
    /// Captures a frame from the specified screen region.
    /// </summary>
    /// <param name="region">
    /// The screen region to capture.
    /// </param>
    /// <returns>
    /// The captured frame, or <see langword="null"/> if a frame
    /// could not be acquired.
    /// </returns>
    Frame? Grab(CaptureRegion region);
}