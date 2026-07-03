namespace NombaCommerceConnect.Application.Orders;

public class PlaceOrderItemRequest
{
    public required Guid ProductId { get; init; }
    public required int Quantity { get; init; }
}

public class PlaceOrderRequest
{
    public required string CustomerEmail { get; init; }
    public required string CustomerFullName { get; init; }
    public required string CallbackUrl { get; init; }
    public required IReadOnlyList<PlaceOrderItemRequest> Items { get; init; }
}

public class PlaceOrderResult
{
    public required Guid OrderId { get; init; }
    public required string OrderReference { get; init; }
    public required string CheckoutLink { get; init; }
    public required decimal TotalAmount { get; init; }
}
