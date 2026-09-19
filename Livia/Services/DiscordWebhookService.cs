using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Livia.Services;

public sealed class DiscordWebhookService : IDisposable
{
    private const string UserAgent = "Livia/1.0";
    private static readonly TimeSpan RequestTimeout = TimeSpan.FromSeconds(15);
    private static readonly TimeSpan DisposeTimeout = TimeSpan.FromSeconds(2);

    private readonly HttpClient _httpClient;
    private readonly string _url;
    private readonly Channel<WebhookMessage> _queue;
    private readonly CancellationTokenSource _cts;
    private readonly Task _worker;

    private bool _disposed;

    public DiscordWebhookService(string url)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(url);

        _url = url;

        _httpClient = new HttpClient
        {
            Timeout = RequestTimeout
        };

        _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(UserAgent);

        _queue = Channel.CreateUnbounded<WebhookMessage>(
            new UnboundedChannelOptions
            {
                SingleReader = true,
                SingleWriter = false
            });

        _cts = new CancellationTokenSource();
        _worker = ProcessQueueAsync();
    }

    public bool Send(
        JsonElement payload,
        byte[]? imageBytes = null,
        string imageFileName = "image.png")
    {
        return Send(
            payload.GetRawText(),
            imageBytes,
            imageFileName);
    }

    public bool Send(
        string payloadJson,
        byte[]? imageBytes = null,
        string imageFileName = "image.png")
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentException.ThrowIfNullOrWhiteSpace(payloadJson);
        ArgumentException.ThrowIfNullOrWhiteSpace(imageFileName);

        return _queue.Writer.TryWrite(
            new WebhookMessage(
                payloadJson,
                imageBytes,
                imageFileName));
    }

    private async Task ProcessQueueAsync()
    {
        try
        {
            while (await _queue.Reader.WaitToReadAsync())
            {
                while (_queue.Reader.TryRead(out WebhookMessage? message))
                {
                    try
                    {
                        await SendMessageAsync(message, _cts.Token);
                    }
                    catch (OperationCanceledException)
                    {
                        return;
                    }
                    catch
                    {

                    }
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Expected during shutdown.
        }
    }

    private async Task SendMessageAsync(
        WebhookMessage message,
        CancellationToken cancellationToken)
    {
        using var form = new MultipartFormDataContent();

        var payloadContent = new StringContent(
            message.PayloadJson,
            Encoding.UTF8,
            "application/json");

        form.Add(payloadContent, "payload_json");

        if (message.ImageBytes is not null)
        {
            var imageContent = new ByteArrayContent(message.ImageBytes);

            imageContent.Headers.ContentType =
                new MediaTypeHeaderValue("image/png");

            form.Add(
                imageContent,
                "files[0]",
                message.ImageFileName);
        }

        using HttpResponseMessage response =
            await _httpClient.PostAsync(
                _url,
                form,
                cancellationToken);

        response.EnsureSuccessStatusCode();
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;

        // Stop accepting new messages.
        _queue.Writer.TryComplete();

        try
        {
            // Give already-queued messages a chance to finish.
            _worker.Wait(DisposeTimeout);
        }
        catch (AggregateException)
        {
            // Ignore worker failures during shutdown.
        }

        // If the worker is still sending something, cancel the request.
        if (!_worker.IsCompleted)
        {
            _cts.Cancel();

            try
            {
                _worker.Wait();
            }
            catch (AggregateException)
            {
                // Ignore cancellation during shutdown.
            }
        }

        _httpClient.Dispose();
        _cts.Dispose();
    }

    private sealed record WebhookMessage(
        string PayloadJson,
        byte[]? ImageBytes,
        string ImageFileName);
}