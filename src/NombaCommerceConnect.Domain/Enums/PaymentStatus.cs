namespace NombaCommerceConnect.Domain.Enums;

/// <summary>
/// State of an individual Nomba payment transaction record tied to an order.
/// </summary>
public enum PaymentStatus
{
    Initiated = 0,
    Pending = 1,
    Success = 2,
    Failed = 3,
    Refunded = 4,
    PartiallyRefunded = 5
}
