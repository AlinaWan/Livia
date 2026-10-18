namespace Livia.Utils.MessageBox;

/// <summary>
/// Enumeration for additional message box options.
/// </summary>
[Flags]
public enum MessageBoxOptions : uint
{
    /// <summary>
    /// No additional options.
    /// </summary>
    None = 0x00000000,

    /// <summary>
    /// Right-aligns the message text.
    /// </summary>
    Right = 0x00080000,

    /// <summary>
    /// Displays text using right-to-left reading order.
    /// </summary>
    RtlReading = 0x00100000,

    /// <summary>
    /// Makes the message box the foreground window.
    /// </summary>
    SetForeground = 0x00010000,

    /// <summary>
    /// Creates the message box with the topmost window style.
    /// </summary>
    Topmost = 0x00040000,

    /// <summary>
    /// Displays the message box on the default desktop.
    /// </summary>
    DefaultDesktopOnly = 0x00020000,

    /// <summary>
    /// Displays a service notification on the active desktop.
    /// </summary>
    ServiceNotification = 0x00200000
}