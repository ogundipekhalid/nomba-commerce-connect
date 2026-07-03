namespace NombaCommerceConnect.Domain.Enums;

/// <summary>
/// Lifecycle states of an order placed against the connector.
/// </summary>
public enum OrderStatus
{
    /// <summary>Order created locally, checkout order not yet requested from Nomba.</summary>
    Created = 0,

    /// <summary>A Nomba checkout order has been created and the customer has a checkout link.</summary>
    PendingPayment = 1,

    /// <summary>Nomba confirmed the payment (via verified webhook + server-side re-check).</summary>
    Paid = 2,

    /// <summary>Payment attempt failed or was cancelled by the customer.</summary>
    Failed = 3,

    /// <summary>Order was fully refunded after being paid.</summary>
    Refunded = 4,

    /// <summary>Order was partially refunded after being paid.</summary>
    PartiallyRefunded = 5
}
