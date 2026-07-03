using System.Text.Json.Serialization;

namespace NombaCommerceConnect.Application.Nomba.Models;

/// <summary>
/// Mirrors the JSON body Nomba posts to a merchant's webhook endpoint. Note the
/// top-level field is snake_case ("event_type") while nested fields are camelCase -
/// explicit JsonPropertyName attributes are used throughout rather than relying on a
/// single naming policy.
/// </summary>
public class NombaWebhookPayload
{
    [JsonPropertyName("event_type")]
    public string EventType { get; init; } = default!;

    [JsonPropertyName("requestId")]
    public string RequestId { get; init; } = default!;

    [JsonPropertyName("data")]
    public NombaWebhookData Data { get; init; } = default!;
}

public class NombaWebhookData
{
    [JsonPropertyName("transaction")]
    public NombaWebhookTransaction Transaction { get; init; } = default!;

    [JsonPropertyName("customer")]
    public NombaWebhookCustomer? Customer { get; init; }

    [JsonPropertyName("order")]
    public NombaWebhookOrder Order { get; init; } = default!;
}

public class NombaWebhookTransaction
{
    [JsonPropertyName("transactionId")]
    public string TransactionId { get; init; } = default!;

    [JsonPropertyName("type")]
    public string? Type { get; init; }

    [JsonPropertyName("transactionAmount")]
    public decimal TransactionAmount { get; init; }

    [JsonPropertyName("fee")]
    public decimal Fee { get; init; }

    [JsonPropertyName("time")]
    public DateTime? Time { get; init; }
}

public class NombaWebhookCustomer
{
    [JsonPropertyName("billerId")]
    public string? BillerId { get; init; }

    [JsonPropertyName("senderName")]
    public string? SenderName { get; init; }
}

public class NombaWebhookOrder
{
    [JsonPropertyName("orderReference")]
    public string OrderReference { get; init; } = default!;

    [JsonPropertyName("amount")]
    public decimal Amount { get; init; }

    [JsonPropertyName("currency")]
    public string Currency { get; init; } = "NGN";

    [JsonPropertyName("paymentMethod")]
    public string? PaymentMethod { get; init; }
}
