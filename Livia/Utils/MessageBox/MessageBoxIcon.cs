namespace Livia.Utils.MessageBox;

/// <summary>
/// Enumeration for message box icons.
/// </summary>
public enum MessageBoxIcon : uint
{
    /// <summary>
    /// Displays no icon.
    /// </summary>
    None = 0x00000000,

    /// <summary>
    /// Displays an error icon.
    /// </summary>
    Error = 0x00000010,

    /// <summary>
    /// Displays a question-mark icon.
    /// </summary>
    Question = 0x00000020,

    /// <summary>
    /// Displays a warning icon.
    /// </summary>
    Warning = 0x00000030,

    /// <summary>
    /// Displays an information icon.
    /// </summary>
    Information = 0x00000040
}