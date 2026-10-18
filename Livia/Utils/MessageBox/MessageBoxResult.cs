namespace Livia.Utils.MessageBox;

/// <summary>
/// Enumeration for message box results.
/// </summary>
public enum MessageBoxResult
{
    /// <summary>
    /// The message box operation failed.
    /// </summary>
    Error = 0,

    OK = 1,
    Cancel = 2,
    Abort = 3,
    Retry = 4,
    Ignore = 5,
    Yes = 6,
    No = 7,
    TryAgain = 10,
    Continue = 11
}