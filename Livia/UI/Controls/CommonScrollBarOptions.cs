namespace Livia.UI.Controls;

/// <summary>
/// Configures the appearance and behavior of a common Livia scrollbar.
/// </summary>
public sealed class CommonScrollBarOptions
{
    /// <summary>
    /// Gets the width of a vertical scrollbar.
    /// </summary>
    public double Width
    {
        get;
        init;
    } = 8;

    /// <summary>
    /// Gets the height of a horizontal scrollbar.
    /// </summary>
    public double Height
    {
        get;
        init;
    } = 8;

    /// <summary>
    /// Gets the corner radius used by the scrollbar track and thumb.
    /// </summary>
    public double CornerRadius
    {
        get;
        init;
    } = 3;

    /// <summary>
    /// Gets the margin around the scrollbar thumb.
    /// </summary>
    public double TrackMargin
    {
        get;
        init;
    } = 1;

    /// <summary>
    /// Gets whether directional arrow buttons are displayed.
    /// </summary>
    public bool ShowArrows
    {
        get;
        init;
    }
}