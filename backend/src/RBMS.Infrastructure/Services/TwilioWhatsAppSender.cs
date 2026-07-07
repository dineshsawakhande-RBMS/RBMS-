using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RBMS.Application.Common.Interfaces;

namespace RBMS.Infrastructure.Services;

/// <summary>
/// Sends WhatsApp messages via Twilio's REST API (works with the free WhatsApp sandbox). Credentials
/// come from <see cref="WhatsAppOptions"/> (user-secrets). Plugged in when <c>WhatsApp:Provider</c>
/// is "Twilio"; the rest of the app is unaffected.
/// </summary>
public class TwilioWhatsAppSender : IWhatsAppSender
{
    private readonly HttpClient _http;
    private readonly WhatsAppOptions _options;
    private readonly ILogger<TwilioWhatsAppSender> _logger;

    public TwilioWhatsAppSender(HttpClient http, IOptions<WhatsAppOptions> options, ILogger<TwilioWhatsAppSender> logger)
    {
        _http = http;
        _options = options.Value;
        _logger = logger;
    }

    public string Provider => "Twilio";

    public async Task<WhatsAppResult> SendAsync(string toPhone, string message, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(_options.AccountSid) || string.IsNullOrWhiteSpace(_options.AuthToken)
            || string.IsNullOrWhiteSpace(_options.FromNumber))
            return new WhatsAppResult(false, null, "Twilio is not configured (WhatsApp:AccountSid/AuthToken/FromNumber).");

        var url = $"https://api.twilio.com/2010-04-01/Accounts/{_options.AccountSid}/Messages.json";
        var request = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["From"] = ToWhatsApp(_options.FromNumber!, _options.DefaultCountryCode),
                ["To"] = ToWhatsApp(toPhone, _options.DefaultCountryCode),
                ["Body"] = message,
            }),
        };
        var basic = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_options.AccountSid}:{_options.AuthToken}"));
        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", basic);

        try
        {
            var response = await _http.SendAsync(request, ct);
            var payload = await response.Content.ReadAsStringAsync(ct);
            using var doc = JsonDocument.Parse(payload);
            var root = doc.RootElement;

            if (response.IsSuccessStatusCode)
            {
                var sid = root.TryGetProperty("sid", out var s) ? s.GetString() : null;
                return new WhatsAppResult(true, sid, null);
            }

            var error = root.TryGetProperty("message", out var m) ? m.GetString() : $"HTTP {(int)response.StatusCode}";
            _logger.LogWarning("Twilio WhatsApp send failed: {Error}", error);
            return new WhatsAppResult(false, null, error);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Twilio WhatsApp send threw");
            return new WhatsAppResult(false, null, ex.Message);
        }
    }

    /// <summary>Normalizes a phone to Twilio's <c>whatsapp:+E164</c> form.</summary>
    internal static string ToWhatsApp(string raw, string defaultCountryCode)
    {
        var trimmed = raw.Trim();
        if (trimmed.StartsWith("whatsapp:", StringComparison.OrdinalIgnoreCase))
            trimmed = trimmed["whatsapp:".Length..];

        var hasPlus = trimmed.TrimStart().StartsWith("+");
        var digits = new string(trimmed.Where(char.IsDigit).ToArray());

        string e164;
        if (hasPlus)
            e164 = "+" + digits;
        else
            e164 = defaultCountryCode + digits.TrimStart('0');

        return "whatsapp:" + e164;
    }
}
