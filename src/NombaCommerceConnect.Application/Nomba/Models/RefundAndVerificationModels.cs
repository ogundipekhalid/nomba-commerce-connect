namespace NombaCommerceConnect.Application.Nomba.Models;

public class RefundRequest
{
    public required string TransactionId { get; init; }

    /// <summary>Null means a full refund of the original transaction amount.</summary>
    public decimal? Amount { get; init; }

    public string? Reason { get; init; }
}

public class RefundResult
{
    public required bool Success { get; init; }
    public required string Message { get; init; }
    public string? RefundReference { get; init; }
}

/// <summary>
/// Result of GET /v1/transactions/accounts/single - used to independently re-verify a
/// transaction's status server-side rather than trusting a webhook payload alone.
/// </summary>
public class TransactionStatusResult
{
    public required string TransactionId { get; init; }
    public required string OrderReference { get; init; }

    /// <summary>Raw status string as returned by Nomba, e.g. "SUCCESS", "FAILED", "PENDING".</summary>
    public required string Status { get; init; }
    public required decimal Amount { get; init; }

    public bool IsSuccessful => string.Equals(Status, "SUCCESS", StringComparison.OrdinalIgnoreCase);
}
