namespace NombaCommerceConnect.Application.Nomba.Models;

/// <summary>
/// Request to POST /v1/checkout/order. Shape mirrors Nomba's documented "order" envelope,
/// including the optional splitRequest used for marketplace payouts.
/// </summary>
public class CreateCheckoutOrderRequest
{
    public required string OrderReference { get; init; }
    public required decimal Amount { get; init; }
    public string Currency { get; init; } = "NGN";
    public required string CustomerEmail { get; init; }
    public string? CustomerId { get; init; }
    public required string CallbackUrl { get; init; }
    public SplitRequestDto? SplitRequest { get; init; }
}

public class SplitRequestDto
{
    /// <summary>"PERCENTAGE" is used here; Nomba also documents flat-value splits for other cases.</summary>
    public string SplitType { get; init; } = "PERCENTAGE";
    public required IReadOnlyList<SplitEntryDto> SplitList { get; init; }
}

public class SplitEntryDto
{
    public required string AccountId { get; init; }
    public required decimal Value { get; init; }
}

public class CheckoutOrderResult
{
    public required string CheckoutLink { get; init; }
    public required string OrderReference { get; init; }
}
