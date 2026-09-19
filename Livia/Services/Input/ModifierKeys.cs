namespace Livia.Services.Input;

public enum ModifierKeys
    : ushort
{
    /// <summary>
    /// Enumeration for modifier keys.
    /// </summary>
    None = 0x0000,
    ModAlt = 0x0001,
    ModControl = 0x0002,
    ModShift = 0x0004,
    ModWindows = 0x0008,
    ModNoRepeat = 0x4000
}