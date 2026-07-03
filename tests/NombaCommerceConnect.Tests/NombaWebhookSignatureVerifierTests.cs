using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NombaCommerceConnect.Infrastructure.Payments.Nomba;
using Xunit;

namespace NombaCommerceConnect.Tests;

public class NombaWebhookSignatureVerifierTests
{
    private const string SigningKey = "test-signing-key-123";

    private static NombaWebhookSignatureVerifier CreateVerifier(string signingKey = SigningKey)
    {
        var options = Options.Create(new NombaOptions { WebhookSigningKey = signingKey });
        return new NombaWebhookSignatureVerifier(options, NullLogger<NombaWebhookSignatureVerifier>.Instance);
    }

    private static string ComputeExpectedSignature(string payload, string key)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(key));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    [Fact]
    public void ValidSignature_IsAccepted()
    {
        var verifier = CreateVerifier();
        var payload = "{\"event_type\":\"payment_success\"}";
        var signature = ComputeExpectedSignature(payload, SigningKey);

        Assert.True(verifier.IsValid(payload, signature));
    }

    [Fact]
    public void TamperedPayload_IsRejected()
    {
        var verifier = CreateVerifier();
        var originalPayload = "{\"event_type\":\"payment_success\",\"data\":{\"order\":{\"amount\":100}}}";
        var signature = ComputeExpectedSignature(originalPayload, SigningKey);

        // Attacker modifies the amount after the signature was computed.
        var tamperedPayload = originalPayload.Replace("100", "100000");

        Assert.False(verifier.IsValid(tamperedPayload, signature));
    }

    [Fact]
    public void MissingSignatureHeader_IsRejected()
    {
        var verifier = CreateVerifier();
        Assert.False(verifier.IsValid("{}", null));
        Assert.False(verifier.IsValid("{}", ""));
    }

    [Fact]
    public void WrongSigningKey_IsRejected()
    {
        var verifier = CreateVerifier();
        var payload = "{\"event_type\":\"payment_success\"}";
        var signatureFromWrongKey = ComputeExpectedSignature(payload, "a-completely-different-key");

        Assert.False(verifier.IsValid(payload, signatureFromWrongKey));
    }

    [Fact]
    public void MissingSigningKeyConfiguration_RejectsEverything()
    {
        var verifier = CreateVerifier(signingKey: "");
        var payload = "{\"event_type\":\"payment_success\"}";
        var signature = ComputeExpectedSignature(payload, SigningKey);

        Assert.False(verifier.IsValid(payload, signature));
    }
}
