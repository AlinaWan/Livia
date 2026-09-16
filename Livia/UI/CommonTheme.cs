using System.Windows.Media;

namespace Livia.UI;

public sealed class CommonTheme
{
    public Color TitleBar
    {
        get; init;
    } =
        Color.FromRgb(32, 32, 32);

    public Color HeaderTop
    {
        get; init;
    } =
        Color.FromRgb(32, 32, 32);

    public Color HeaderBottom
    {
        get; init;
    } =
        Color.FromRgb(24, 24, 24);

    public Color Background
    {
        get; init;
    } =
        Color.FromRgb(24, 24, 24);

    public Color HeaderBorder
    {
        get; init;
    } =
        Color.FromArgb(60, 220, 220, 220);

    public Color PrimaryText
    {
        get; init;
    } =
        Color.FromRgb(235, 235, 235);

    public Color SecondaryText
    {
        get; init;
    } =
        Color.FromRgb(180, 180, 180);

    public Color MutedText
    {
        get; init;
    } =
        Color.FromRgb(130, 130, 130);

    public Color Surface
    {
        get; init;
    } =
        Color.FromRgb(30, 30, 30);

    public Color SurfaceDark
    {
        get; init;
    } =
        Color.FromRgb(18, 18, 18);

    public Color Border
    {
        get; init;
    } =
        Color.FromRgb(45, 45, 45);

    public Color InputBackground
    {
        get; init;
    } =
        Color.FromRgb(36, 36, 36);

    public Color InputBorder
    {
        get; init;
    } =
        Color.FromRgb(55, 55, 55);

    public Color Accent
    {
        get; init;
    } =
        Color.FromRgb(61, 35, 20);

    public Color AccentText
    {
        get; init;
    } =
        Color.FromRgb(230, 200, 170);

    public Color Success
    {
        get; init;
    } =
        Color.FromRgb(40, 90, 50);

    public Color Error
    {
        get; init;
    } =
        Color.FromRgb(180, 40, 50);
}