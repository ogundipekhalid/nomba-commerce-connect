namespace NombaCommerceConnect.Infrastructure.Payments.Nomba;

/// <summary>
/// Bound from the "Nomba" configuration section (appsettings.json / environment
/// variables / user-secrets). None of these have real values checked into source -
/// see README for how to supply them locally and in the sample docker-compose file.
/// </summary>
public class NombaOptions
{
    public const string SectionName = "Nomba";

    public string BaseUrl { get; set; } = "https://sandbox.nomba.com";
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
    public string AccountId { get; set; } = string.Empty;

    /// <summary>Signing key configured on the Nomba dashboard for this webhook endpoint.</summary>
    public string WebhookSigningKey { get; set; } = string.Empty;

    /// <summary>
    /// Name of the header Nomba sends the webhook signature in. Kept configurable
    /// since the exact header name should be confirmed against the dashboard once
    /// webhook credentials are provisioned.
    /// </summary>
    public string WebhookSignatureHeaderName { get; set; } = "nomba-sig-value";

    /// <summary>
    /// When true, the DI container registers <see cref="FakeNombaClient"/> instead of
    /// the real HTTP-backed client - used until live sandbox credentials arrive.
    /// </summary>
    public bool UseFakeClient { get; set; } = true;

    /// <summary>
    /// Path for refunding a checkout transaction. Nomba's docs nav lists a
    /// "Refund checkout transaction" endpoint under Online Checkout, but the exact
    /// path was not confirmed without full API reference/console access at the time
    /// this was written. Confirm and update once credentials/full docs access arrive -
    /// called out again in the README.
    /// </summary>
    public string RefundEndpointPath { get; set; } = "/v1/checkout/transaction/refund";

    /// <summary>Path used to verify a transaction's status server-side.</summary>
    public string VerifyTransactionEndpointPath { get; set; } = "/v1/transactions/accounts/single";
}
