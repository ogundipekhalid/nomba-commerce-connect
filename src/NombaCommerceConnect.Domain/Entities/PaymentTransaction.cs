using NombaCommerceConnect.Domain.Enums;

namespace NombaCommerceConnect.Domain.Entities;

/// <summary>
/// An audit trail record of a Nomba payment lifecycle event (checkout initiated,
/// webhook received, refund issued) tied to an order. Storing the Nomba
/// <c>requestId</c> lets the webhook handler detect and ignore duplicate deliveries.
/// </summary>
public class PaymentTransaction
{
    public Guid Id { get; private set; }
    public Guid OrderId { get; private set; }

    public string? NombaTransactionId { get; private set; }

    /// <summary>The `requestId` from the Nomba webhook payload, used for idempotency checks.</summary>
    public string? NombaRequestId { get; private set; }

    public string EventType { get; private set; } = default!;
    public PaymentStatus Status { get; private set; }
    public decimal Amount { get; private set; }
    public string RawPayload { get; private set; } = string.Empty;
    public DateTime CreatedAtUtc { get; private set; }

    private PaymentTransaction() { }

    public static PaymentTransaction FromWebhook(
        Guid orderId,
        string eventType,
        string? nombaTransactionId,
        string? nombaRequestId,
        decimal amount,
        PaymentStatus status,
        string rawPayload)
    {
        if (orderId == Guid.Empty)
            throw new ArgumentException("A transaction must be tied to an order.", nameof(orderId));
        if (string.IsNullOrWhiteSpace(eventType))
            throw new ArgumentException("Event type is required.", nameof(eventType));

        return new PaymentTransaction
        {
            Id = Guid.NewGuid(),
            OrderId = orderId,
            EventType = eventType,
            NombaTransactionId = nombaTransactionId,
            NombaRequestId = nombaRequestId,
            Amount = amount,
            Status = status,
            RawPayload = rawPayload ?? string.Empty,
            CreatedAtUtc = DateTime.UtcNow
        };
    }

    public static PaymentTransaction ForRefund(Guid orderId, string nombaTransactionId, decimal amount, PaymentStatus status)
    {
        return new PaymentTransaction
        {
            Id = Guid.NewGuid(),
            OrderId = orderId,
            EventType = "refund",
            NombaTransactionId = nombaTransactionId,
            Amount = amount,
            Status = status,
            CreatedAtUtc = DateTime.UtcNow
        };
    }
}
