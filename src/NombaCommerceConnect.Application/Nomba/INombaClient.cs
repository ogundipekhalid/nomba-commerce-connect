using NombaCommerceConnect.Application.Nomba.Models;

namespace NombaCommerceConnect.Application.Nomba;

/// <summary>
/// Abstraction over the Nomba HTTP API. Implemented for real in Infrastructure
/// (<c>NombaClient</c>) and mocked (<c>FakeNombaClient</c>) so the rest of the system
/// can be built, tested, and demoed before live sandbox credentials are available.
/// </summary>
public interface INombaClient
{
    Task<CheckoutOrderResult> CreateCheckoutOrderAsync(CreateCheckoutOrderRequest request, CancellationToken ct = default);

    Task<TransactionStatusResult> VerifyTransactionAsync(string orderReference, CancellationToken ct = default);

    Task<RefundResult> RefundTransactionAsync(RefundRequest request, CancellationToken ct = default);
}

/// <summary>
/// Verifies the authenticity of an inbound Nomba webhook by recomputing its signature
/// and comparing it to the one supplied in the request header.
/// </summary>
public interface INombaWebhookSignatureVerifier
{
    bool IsValid(string rawRequestBody, string? signatureHeaderValue);
}

/// <summary>
/// Thrown when Nomba's API returns a non-success response. Carries the structured
/// code/description Nomba provides so callers can surface a meaningful error state
/// instead of a generic HTTP failure.
/// </summary>
public class NombaApiException : Exception
{
    public string? NombaCode { get; }
    public int? HttpStatusCode { get; }

    public NombaApiException(string message, string? nombaCode = null, int? httpStatusCode = null, Exception? innerException = null)
        : base(message, innerException)
    {
        NombaCode = nombaCode;
        HttpStatusCode = httpStatusCode;
    }
}
