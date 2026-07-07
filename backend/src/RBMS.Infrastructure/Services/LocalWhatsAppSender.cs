using Microsoft.Extensions.Logging;
using RBMS.Application.Common.Interfaces;

namespace RBMS.Infrastructure.Services;

public class WhatsAppOptions
{
    public const string SectionName = "WhatsApp";
    /// <summary>"Local" (default, logs only) or "Twilio".</summary>
    public string Provider { get; set; } = "Local";

    // --- Twilio (set via user-secrets when Provider = "Twilio") ---
    public string? AccountSid { get; set; }
    public string? AuthToken { get; set; }
    /// <summary>Twilio WhatsApp sender, e.g. the sandbox number "+14155238886".</summary>
    public string? FromNumber { get; set; }
    /// <summary>Prefixed to bare local numbers that lack a country code (India by default).</summary>
    public string DefaultCountryCode { get; set; } = "+91";
}

/// <summary>
/// Local stub for <see cref="IWhatsAppSender"/>: logs the message and reports success without any
/// external call. Lets the whole WhatsApp flow work end-to-end on-prem with no credentials; swap
/// the DI registration for a real provider when the shop is ready.
/// </summary>
public class LocalWhatsAppSender : IWhatsAppSender
{
    private readonly ILogger<LocalWhatsAppSender> _logger;
    public LocalWhatsAppSender(ILogger<LocalWhatsAppSender> logger) => _logger = logger;

    public string Provider => "LocalStub";

    public Task<WhatsAppResult> SendAsync(string toPhone, string message, CancellationToken ct = default)
    {
        _logger.LogInformation("[WhatsApp:LocalStub] to {Phone}: {Message}", toPhone, message);
        return Task.FromResult(new WhatsAppResult(true, $"local-{Guid.NewGuid():N}", null));
    }
}
