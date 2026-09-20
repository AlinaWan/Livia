using Windows.Graphics.Imaging;
using Windows.Media.Ocr;
using Windows.Storage.Streams;

namespace Livia.Services.Ocr;

/// <summary>
/// Provides OCR using the native Windows OCR engine.
/// </summary>
public sealed class WindowsOcrService
{
    private readonly OcrEngine _engine;

    /// <summary>
    /// Initializes a new instance of the <see cref="WindowsOcrService"/> class
    /// using the OCR languages configured for the current user.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown when Windows cannot create an OCR engine for the user's configured languages.
    /// </exception>
    public WindowsOcrService()
    {
        _engine = OcrEngine.TryCreateFromUserProfileLanguages()
            ?? throw new InvalidOperationException(
                "Windows OCR is unavailable for the user's configured languages.");
    }

    /// <summary>
    /// Runs Windows OCR on a decoded software bitmap.
    /// </summary>
    /// <param name="bitmap">The image to recognize.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    /// <returns>The text recognized by Windows OCR.</returns>
    public async Task<string> RecognizeAsync(
        SoftwareBitmap bitmap,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(bitmap);

        cancellationToken.ThrowIfCancellationRequested();

        OcrResult result = await _engine
            .RecognizeAsync(bitmap)
            .AsTask(cancellationToken);

        return result.Text;
    }

    /// <summary>
    /// Runs Windows OCR on an encoded image.
    /// </summary>
    /// <param name="imageBytes">
    /// Encoded image bytes supported by the Windows imaging APIs.
    /// </param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    /// <returns>The text recognized by Windows OCR.</returns>
    public async Task<string> RecognizeAsync(
        ReadOnlyMemory<byte> imageBytes,
        CancellationToken cancellationToken = default)
    {
        if (imageBytes.IsEmpty)
        {
            throw new ArgumentException(
                "Image data cannot be empty.",
                nameof(imageBytes));
        }

        using InMemoryRandomAccessStream stream = new();

        using (DataWriter writer = new(stream))
        {
            writer.WriteBytes(imageBytes.Span.ToArray());
            await writer.StoreAsync().AsTask(cancellationToken);
        }

        stream.Seek(0);

        BitmapDecoder decoder = await BitmapDecoder
            .CreateAsync(stream)
            .AsTask(cancellationToken);

        using SoftwareBitmap bitmap = await decoder
            .GetSoftwareBitmapAsync()
            .AsTask(cancellationToken);

        return await RecognizeAsync(bitmap, cancellationToken);
    }
}