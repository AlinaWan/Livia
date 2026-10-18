using System.Windows;
using System.Windows.Interop;
using System.Windows.Threading;
using Livia.Native;
using Livia.Utils.MessageBox;
using MessageBoxResult = Livia.Utils.MessageBox.MessageBoxResult;
using MessageBoxOptions = Livia.Utils.MessageBox.MessageBoxOptions;

namespace Livia.Utils;

/// <summary>
/// Provides methods for displaying native Windows message boxes.
/// </summary>
public static class MessageBoxUtils
{
    /// <summary>
    /// Displays a native Windows message box.
    /// </summary>
    /// <param name="text">The message to display.</param>
    /// <param name="title">The message box title.</param>
    /// <param name="buttons">The buttons to display.</param>
    /// <param name="icon">The icon to display.</param>
    /// <param name="defaultButton">The default button.</param>
    /// <param name="modality">The modality of the message box.</param>
    /// <param name="options">The options for the message box.</param>
    /// <param name="owner">The handle of the owner window, or zero if there is no owner.</param>
    /// <returns>The button selected by the user.</returns>
    public static MessageBoxResult Show(
        string? text,
        string? title,
        MessageBoxButtons buttons = MessageBoxButtons.OK,
        MessageBoxIcon icon = MessageBoxIcon.None,
        MessageBoxDefaultButton defaultButton = MessageBoxDefaultButton.Button1,
        MessageBoxModality modality = MessageBoxModality.Application,
        MessageBoxOptions options = MessageBoxOptions.None,
        nint owner = 0)
    {
        if (options.HasFlag(MessageBoxOptions.ServiceNotification) && owner != 0)
        {
            throw new ArgumentException(
                "The owner handle must be zero when ServiceNotification is specified.",
                nameof(owner));
        }

        uint flags = (uint)buttons | (uint)icon | (uint)defaultButton | (uint)modality | (uint)options;

        return (MessageBoxResult)User32.MessageBoxW(
            owner,
            text,
            title,
            flags);
    }

    /// <summary>
    /// Displays a native Windows message box asynchronously.
    /// </summary>
    /// <param name="text">The message to display.</param>
    /// <param name="title">The message box title.</param>
    /// <param name="buttons">The buttons to display.</param>
    /// <param name="icon">The icon to display.</param>
    /// <param name="defaultButton">The default button.</param>
    /// <param name="modality">The modality of the message box.</param>
    /// <param name="options">The options for the message box.</param>
    /// <param name="owner">The handle of the owner window, or zero if there is no owner.</param>
    /// <param name="cancellationToken">A token used to cancel the operation before it starts.</param>
    /// <returns>A task containing the button selected by the user.</returns>
    public static Task<MessageBoxResult> ShowAsync(
        string? text,
        string? title,
        MessageBoxButtons buttons = MessageBoxButtons.OK,
        MessageBoxIcon icon = MessageBoxIcon.None,
        MessageBoxDefaultButton defaultButton = MessageBoxDefaultButton.Button1,
        MessageBoxModality modality = MessageBoxModality.Application,
        MessageBoxOptions options = MessageBoxOptions.None,
        nint owner = 0,
        CancellationToken cancellationToken = default)
    {
        return Task.Run(
            () => Show(text, title, buttons, icon, defaultButton, modality, options, owner),
            cancellationToken);
    }

    /// <inheritdoc cref="ShowAsync(string, string, MessageBoxButtons, MessageBoxIcon, MessageBoxDefaultButton, MessageBoxModality, MessageBoxOptions, nint, CancellationToken)"/>
    public static async Task<MessageBoxResult> ShowAsync(
        Window owner,
        string? text,
        string? title,
        MessageBoxButtons buttons = MessageBoxButtons.OK,
        MessageBoxIcon icon = MessageBoxIcon.None,
        MessageBoxDefaultButton defaultButton = MessageBoxDefaultButton.Button1,
        MessageBoxModality modality = MessageBoxModality.Application,
        MessageBoxOptions options = MessageBoxOptions.None,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(owner);

        if (options.HasFlag(MessageBoxOptions.ServiceNotification))
        {
            throw new ArgumentException(
                "ServiceNotification cannot be used with a WPF owner window.",
                nameof(options));
        }

        cancellationToken.ThrowIfCancellationRequested();

        return await owner.Dispatcher.InvokeAsync(
            () =>
            {
                if (!owner.IsVisible)
                {
                    throw new InvalidOperationException(
                        "The owner window must be visible.");
                }

                nint handle = new WindowInteropHelper(owner).Handle;

                return Show(
                    text,
                    title,
                    buttons,
                    icon,
                    defaultButton,
                    modality,
                    options,
                    handle);
            },
            DispatcherPriority.Normal,
            cancellationToken);
    }
}