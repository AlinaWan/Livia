namespace Livia.Services.ScreenCapture;

/// <summary>
/// Defines a rectangular region of the screen to capture.
/// </summary>
public readonly record struct CaptureRegion(
    int Left,
    int Top,
    int Right,
    int Bottom)
{
    /// <summary>
    /// Gets the width of the region in pixels.
    /// </summary>
    public int Width => Right - Left;

    /// <summary>
    /// Gets the height of the region in pixels.
    /// </summary>
    public int Height => Bottom - Top;
}