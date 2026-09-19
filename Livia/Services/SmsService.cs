using System;
using System.Net;
using System.Net.Mail;
using System.Threading;
using System.Threading.Tasks;

namespace Livia.Services;

public sealed class SmsService : IDisposable
{
    private readonly SmtpClient _smtpClient;
    private readonly string _gatewayAddress;
    private readonly string _fromAddress;
    private readonly string _defaultSubject;
    private bool _disposed;

    public SmsService(
        string smtpHost,
        int smtpPort,
        string smtpUsername,
        string smtpPassword,
        string fromAddress,
        string gatewayAddress,
        string defaultSubject = "")
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(smtpHost);
        ArgumentException.ThrowIfNullOrWhiteSpace(smtpUsername);
        ArgumentException.ThrowIfNullOrWhiteSpace(smtpPassword);
        ArgumentException.ThrowIfNullOrWhiteSpace(fromAddress);
        ArgumentException.ThrowIfNullOrWhiteSpace(gatewayAddress);

        _gatewayAddress = gatewayAddress.Trim().TrimStart('@');
        _fromAddress = fromAddress;
        _defaultSubject = defaultSubject ?? string.Empty;

        _smtpClient = new SmtpClient(smtpHost, smtpPort)
        {
            Credentials = new NetworkCredential(
                smtpUsername,
                smtpPassword),

            EnableSsl = true
        };
    }

    public async Task SendAsync(
        string number,
        string message,
        string? subject = null,
        CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentException.ThrowIfNullOrWhiteSpace(number);
        ArgumentException.ThrowIfNullOrWhiteSpace(message);

        number = number.Trim().TrimStart('+');

        if (number.Length == 0)
            throw new ArgumentException(
                "The phone number must contain at least one digit.",
                nameof(number));

        var recipient = $"{number}@{_gatewayAddress}";

        using var mail = new MailMessage(
            _fromAddress,
            recipient,
            subject ?? _defaultSubject,
            message);

        await _smtpClient.SendMailAsync(
            mail,
            cancellationToken);
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _smtpClient.Dispose();
        _disposed = true;
    }
}