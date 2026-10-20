namespace Livia.Utils;

/// <summary>
/// Enumeration for system metrics parameter values.
/// </summary>
public enum SystemMetric
{
    /// <summary>
    /// The flags that specify how the system arranges minimized windows.
    /// </summary>
    Arrange = 56,

    /// <summary>
    /// The current boot mode: normal, fail-safe, or fail-safe with networking.
    /// </summary>
    CleanBoot = 67,

    /// <summary>
    /// The number of visible display monitors.
    /// </summary>
    MonitorCount = 80,

    /// <summary>
    /// The number of mouse buttons, or zero if no mouse is installed.
    /// </summary>
    MouseButtonCount = 43,

    /// <summary>
    /// Whether the system is in slate mode.
    /// </summary>
    ConvertibleSlateMode = 0x2003,

    /// <summary>
    /// The width of a window border, in pixels.
    /// </summary>
    BorderWidth = 5,

    /// <summary>
    /// The height of a window border, in pixels.
    /// </summary>
    BorderHeight = 6,

    /// <summary>
    /// The nominal width of a cursor, in pixels.
    /// </summary>
    CursorWidth = 13,

    /// <summary>
    /// The nominal height of a cursor, in pixels.
    /// </summary>
    CursorHeight = 14,

    /// <summary>
    /// The width of a dialog frame, in pixels.
    /// </summary>
    DialogFrameWidth = 7,

    /// <summary>
    /// The height of a dialog frame, in pixels.
    /// </summary>
    DialogFrameHeight = 8,

    /// <summary>
    /// The width of the double-click rectangle, in pixels.
    /// </summary>
    DoubleClickWidth = 36,

    /// <summary>
    /// The height of the double-click rectangle, in pixels.
    /// </summary>
    DoubleClickHeight = 37,

    /// <summary>
    /// The horizontal distance the pointer can move before a drag begins, in pixels.
    /// </summary>
    DragWidth = 68,

    /// <summary>
    /// The vertical distance the pointer can move before a drag begins, in pixels.
    /// </summary>
    DragHeight = 69,

    /// <summary>
    /// The width of a three-dimensional border, in pixels.
    /// </summary>
    EdgeWidth = 45,

    /// <summary>
    /// The height of a three-dimensional border, in pixels.
    /// </summary>
    EdgeHeight = 46,

    /// <summary>
    /// The width of the focus rectangle border, in pixels.
    /// </summary>
    FocusBorderWidth = 83,

    /// <summary>
    /// The height of the focus rectangle border, in pixels.
    /// </summary>
    FocusBorderHeight = 84,

    /// <summary>
    /// The width of the fixed window frame, in pixels.
    /// </summary>
    FixedFrameWidth = 7,

    /// <summary>
    /// The height of the fixed window frame, in pixels.
    /// </summary>
    FixedFrameHeight = 8,

    /// <summary>
    /// The width of the client area of a full-screen window, in pixels.
    /// </summary>
    FullScreenWidth = 16,

    /// <summary>
    /// The height of the client area of a full-screen window, in pixels.
    /// </summary>
    FullScreenHeight = 17,

    /// <summary>
    /// The width of a horizontal scroll bar, in pixels.
    /// </summary>
    HorizontalScrollBarWidth = 21,

    /// <summary>
    /// The height of a horizontal scroll bar, in pixels.
    /// </summary>
    HorizontalScrollBarHeight = 3,

    /// <summary>
    /// The width of the horizontal scroll bar thumb, in pixels.
    /// </summary>
    HorizontalScrollThumbWidth = 10,

    /// <summary>
    /// The width of a vertical scroll bar, in pixels.
    /// </summary>
    VerticalScrollBarWidth = 2,

    /// <summary>
    /// The height of a vertical scroll bar, in pixels.
    /// </summary>
    VerticalScrollBarHeight = 20,

    /// <summary>
    /// The height of the vertical scroll bar thumb, in pixels.
    /// </summary>
    VerticalScrollThumbHeight = 9,

    /// <summary>
    /// The width of a large icon, in pixels.
    /// </summary>
    IconWidth = 11,

    /// <summary>
    /// The height of a large icon, in pixels.
    /// </summary>
    IconHeight = 12,

    /// <summary>
    /// The width of a small icon, in pixels.
    /// </summary>
    SmallIconWidth = 49,

    /// <summary>
    /// The height of a small icon, in pixels.
    /// </summary>
    SmallIconHeight = 50,

    /// <summary>
    /// The width of the grid cell for items in large icon view, in pixels.
    /// </summary>
    IconSpacingWidth = 38,

    /// <summary>
    /// The height of the grid cell for items in large icon view, in pixels.
    /// </summary>
    IconSpacingHeight = 39,

    /// <summary>
    /// The width of a maximized top-level window, in pixels.
    /// </summary>
    MaximizedWidth = 61,

    /// <summary>
    /// The height of a maximized top-level window, in pixels.
    /// </summary>
    MaximizedHeight = 62,

    /// <summary>
    /// The maximum width of a resizable window, in pixels.
    /// </summary>
    MaximumTrackingWidth = 59,

    /// <summary>
    /// The maximum height of a resizable window, in pixels.
    /// </summary>
    MaximumTrackingHeight = 60,

    /// <summary>
    /// The width of a menu check mark, in pixels.
    /// </summary>
    MenuCheckWidth = 71,

    /// <summary>
    /// The height of a menu check mark, in pixels.
    /// </summary>
    MenuCheckHeight = 72,

    /// <summary>
    /// The width of a menu bar button, in pixels.
    /// </summary>
    MenuButtonWidth = 54,

    /// <summary>
    /// The height of a menu bar button, in pixels.
    /// </summary>
    MenuButtonHeight = 55,

    /// <summary>
    /// The minimum width of a window, in pixels.
    /// </summary>
    MinimumWindowWidth = 28,

    /// <summary>
    /// The minimum height of a window, in pixels.
    /// </summary>
    MinimumWindowHeight = 29,

    /// <summary>
    /// The width of a minimized window, in pixels.
    /// </summary>
    MinimizedWidth = 57,

    /// <summary>
    /// The height of a minimized window, in pixels.
    /// </summary>
    MinimizedHeight = 58,

    /// <summary>
    /// The width of the grid cell for a minimized window, in pixels.
    /// </summary>
    MinimizedSpacingWidth = 47,

    /// <summary>
    /// The height of the grid cell for a minimized window, in pixels.
    /// </summary>
    MinimizedSpacingHeight = 48,

    /// <summary>
    /// The minimum tracking width of a resizable window, in pixels.
    /// </summary>
    MinimumTrackingWidth = 34,

    /// <summary>
    /// The minimum tracking height of a resizable window, in pixels.
    /// </summary>
    MinimumTrackingHeight = 35,

    /// <summary>
    /// The amount of border padding around captioned windows, in pixels.
    /// </summary>
    PaddedBorderWidth = 92,

    /// <summary>
    /// The width of the primary display monitor, in pixels.
    /// </summary>
    ScreenWidth = 0,

    /// <summary>
    /// The height of the primary display monitor, in pixels.
    /// </summary>
    ScreenHeight = 1,

    /// <summary>
    /// The width of a window caption button, in pixels.
    /// </summary>
    CaptionButtonWidth = 30,

    /// <summary>
    /// The height of a window caption button, in pixels.
    /// </summary>
    CaptionButtonHeight = 31,

    /// <summary>
    /// The width of the sizing frame around a resizable window, in pixels.
    /// </summary>
    SizingFrameWidth = 32,

    /// <summary>
    /// The height of the sizing frame around a resizable window, in pixels.
    /// </summary>
    SizingFrameHeight = 33,

    /// <summary>
    /// The width of a small caption button, in pixels.
    /// </summary>
    SmallCaptionButtonWidth = 52,

    /// <summary>
    /// The height of a small caption button, in pixels.
    /// </summary>
    SmallCaptionButtonHeight = 53,

    /// <summary>
    /// The height of a small window caption, in pixels.
    /// </summary>
    SmallCaptionHeight = 51,

    /// <summary>
    /// The width of the virtual screen, in pixels.
    /// </summary>
    VirtualScreenWidth = 78,

    /// <summary>
    /// The height of the virtual screen, in pixels.
    /// </summary>
    VirtualScreenHeight = 79,

    /// <summary>
    /// The left coordinate of the virtual screen, in pixels.
    /// </summary>
    VirtualScreenLeft = 76,

    /// <summary>
    /// The top coordinate of the virtual screen, in pixels.
    /// </summary>
    VirtualScreenTop = 77,

    /// <summary>
    /// Whether the system supports double-byte character sets.
    /// </summary>
    DoubleByteCharacterSetEnabled = 42,

    /// <summary>
    /// Whether the debug version of Windows is installed.
    /// </summary>
    Debug = 22,

    /// <summary>
    /// The digitizer capabilities of the system.
    /// </summary>
    Digitizer = 94,

    /// <summary>
    /// Whether an input method editor is enabled.
    /// </summary>
    InputMethodEditorEnabled = 82,

    /// <summary>
    /// The maximum number of simultaneous touch contacts supported by the system.
    /// </summary>
    MaximumTouchContacts = 95,

    /// <summary>
    /// Whether the system is running Windows XP Media Center Edition.
    /// </summary>
    MediaCenter = 87,

    /// <summary>
    /// Whether drop-down menus are right-aligned.
    /// </summary>
    MenuDropAlignment = 40,

    /// <summary>
    /// Whether the system is configured for Middle Eastern languages.
    /// </summary>
    MiddleEastEnabled = 74,

    /// <summary>
    /// Whether a mouse is installed.
    /// </summary>
    MousePresent = 19,

    /// <summary>
    /// Whether a mouse with a horizontal scroll wheel is installed.
    /// </summary>
    HorizontalMouseWheelPresent = 91,

    /// <summary>
    /// Whether a mouse with a vertical scroll wheel is installed.
    /// </summary>
    VerticalMouseWheelPresent = 75,

    /// <summary>
    /// Whether a network is present.
    /// </summary>
    NetworkPresent = 63,

    /// <summary>
    /// Whether Windows for Pen computing extensions are installed.
    /// </summary>
    PenWindows = 41,

    /// <summary>
    /// Whether the current Terminal Services session is being remotely controlled.
    /// </summary>
    RemoteControl = 0x2001,

    /// <summary>
    /// Whether the current process is associated with a Terminal Services client session.
    /// </summary>
    RemoteSession = 0x1000,

    /// <summary>
    /// Whether all display monitors use the same color format.
    /// </summary>
    SameDisplayFormat = 81,

    /// <summary>
    /// A reserved metric that always returns zero.
    /// </summary>
    Secure = 44,

    /// <summary>
    /// The Windows Server 2003 R2 build number, or zero otherwise.
    /// </summary>
    ServerR2 = 89,

    /// <summary>
    /// Whether the user requires information to be presented visually instead of only audibly.
    /// </summary>
    ShowSounds = 70,

    /// <summary>
    /// Whether the current session is shutting down.
    /// </summary>
    ShuttingDown = 0x2000,

    /// <summary>
    /// Whether the system has a low-end processor.
    /// </summary>
    SlowMachine = 73,

    /// <summary>
    /// Whether the system is running a Windows Starter edition.
    /// </summary>
    Starter = 88,

    /// <summary>
    /// Whether the left and right mouse buttons are swapped.
    /// </summary>
    SwapMouseButtons = 23,

    /// <summary>
    /// Whether the system is docked.
    /// </summary>
    SystemDocked = 0x2004,

    /// <summary>
    /// Whether the system is running a supported Tablet PC edition.
    /// </summary>
    TabletPC = 86,

    /// <summary>
    /// The width of a horizontal scroll bar thumb, in pixels.
    /// </summary>
    HorizontalScrollThumbHeight = 10
}