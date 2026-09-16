using System.Windows;

namespace Common;

public sealed class CommonWindowOptions
{
    public string Title { get; init; } = string.Empty;

    public string? Header
    {
        get; init;
    }

    public string? HeaderIcon
    {
        get; init;
    }

    public string? Author
    {
        get; init;
    }

    public double Width { get; init; } = 420;

    public double Height { get; init; } = 410;

    public bool Topmost
    {
        get; init;
    }

    public WindowStartupLocation StartupLocation
    {
        get; init;
    } =
        WindowStartupLocation.CenterScreen;

    public CommonTheme Theme { get; init; } = new();
}