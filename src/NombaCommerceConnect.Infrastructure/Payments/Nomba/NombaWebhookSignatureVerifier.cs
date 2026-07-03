using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NombaCommerceConnect.Application.Nomba;

namespace NombaCommerceConnect.Infrastructure.Payments.Nomba;

/// <summary>
/// Verifies inbound Nomba webhooks by recomputing an HMAC-SHA256 signature over the
/// raw request body using the dashboard-configured signing key, and comparing it to
/// the signature Nomba supplies in the request header - using a constant-time
/// comparison to avoid timing attacks.
///
/// NOTE: the exact signing scheme (which header, whether the timestamp/other fields
/// are folded into the signed string, hex vs base64 encoding) should be confirmed
/// against the "Setting up webhooks" guide once the merchant's webhook signing key is
/// provisioned on the dashboard. This implementation follows the general pattern
/// documented (HMAC-SHA256 over the raw payload) and is unit tested against that
/// assumption so swapping in the confirmed exact scheme is a one-file change.
/// </summary>
public class NombaWebhookSignatureVerifier : INombaWebhookSignatureVerifier
{
    private readonly NombaOptions _options;
    private readonly ILogger<NombaWebhookSignatureVerifier> _logger;

    public NombaWebhookSignatureVerifier(IOptions<NombaOptions> options, ILogger<NombaWebhookSignatureVerifier> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public bool IsValid(string rawRequestBody, string? signatureHeaderValue)
    {
        if (string.IsNullOrWhiteSpace(signatureHeaderValue))
        {
            _logger.LogWarning("Rejected webhook: no signature header present.");
            return false;
        }

        if (string.IsNullOrWhiteSpace(_options.WebhookSigningKey))
        {
            _logger.LogError("Webhook signing key is not configured - cannot verify inbound webhooks.");
            return false;
        }

        var expectedSignature = ComputeSignature(rawRequestBody, _options.WebhookSigningKey);
        return FixedTimeEquals(expectedSignature, signatureHeaderValue.Trim());
    }

    private static string ComputeSignature(string payload, string signingKey)
    {
        var keyBytes = Encoding.UTF8.GetBytes(signingKey);
        var payloadBytes = Encoding.UTF8.GetBytes(payload);

        using var hmac = new HMACSHA256(keyBytes);
        var hashBytes = hmac.ComputeHash(payloadBytes);
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }

    private static bool FixedTimeEquals(string a, string b)
    {
        var aBytes = Encoding.UTF8.GetBytes(a);
        var bBytes = Encoding.UTF8.GetBytes(b);

        if (aBytes.Length != bBytes.Length)
            return false;

        return CryptographicOperations.FixedTimeEquals(aBytes, bBytes);
    }
}
