namespace Livia.Utils.MessageBox;

/// <summary>
/// Enumeration for message box buttons.
/// </summary>
public enum MessageBoxButtons : uint
{
    /// <summary>
    /// Displays an OK button.
    /// </summary>
    OK = 0x00000000,

    /// <summary>
    /// Displays OK and Cancel buttons.
    /// </summary>
    OKCancel = 0x00000001,

    /// <summary>
    /// Displays Abort, Retry, and Ignore buttons.
    /// </summary>
    AbortRetryIgnore = 0x00000002,

    /// <summary>
    /// Displays Yes, No, and Cancel buttons.
    /// </summary>
    YesNoCancel = 0x00000003,

    /// <summary>
    /// Displays Yes and No buttons.
    /// </summary>
    YesNo = 0x00000004,

    /// <summary>
    /// Displays Retry and Cancel buttons.
    /// </summary>
    RetryCancel = 0x00000005,

    /// <summary>
    /// Displays Cancel, Try Again, and Continue buttons.
    /// </summary>
    CancelTryContinue = 0x00000006,

    /// <summary>
    /// Displays a Help button.
    /// </summary>
    Help = 0x00004000
}