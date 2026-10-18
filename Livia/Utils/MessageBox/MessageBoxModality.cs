namespace Livia.Utils.MessageBox;

/// <summary>
/// Enumeration for message box modalities.
/// </summary>
public enum MessageBoxModality : uint
{
    /// <summary>
    /// The user must respond before continuing work in the owner window.
    /// </summary>
    Application = 0x00000000,

    /// <summary>
    /// Disables the top-level windows belonging to the calling thread
    /// when the owner handle is zero.
    /// </summary>
    Task = 0x00002000,

    /// <summary>
    /// Displays the message box as a topmost window.
    /// </summary>
    System = 0x00001000
}